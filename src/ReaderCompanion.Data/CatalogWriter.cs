using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

/// <summary>Applies scan actions to the tracked catalog: files, books, and shared authors, genres and series.</summary>
internal sealed class CatalogWriter
{
    private readonly LibraryDbContext _db;
    private readonly string _coversFolder;
    private readonly DateTime _now;
    private readonly Dictionary<int, BookFile> _files;
    private readonly Dictionary<int, bool> _bookUnreadable;
    private readonly Dictionary<string, Author> _authors;
    private readonly Dictionary<string, Genre> _genres;
    private readonly Dictionary<string, Collection> _series;
    private readonly List<UnreadableFile> _unreadable = [];

    public CatalogWriter(LibraryDbContext db, string coversFolder, DateTime now)
    {
        _db = db;
        _coversFolder = coversFolder;
        _now = now;
        _files = db.BookFiles.ToDictionary(f => f.Id);
        _bookUnreadable = _files.Values.GroupBy(f => f.BookId).ToDictionary(g => g.Key, g => g.First().Unreadable);
        _authors = db.Authors.ToDictionary(a => a.NormalizedName);
        _genres = db.Genres.ToDictionary(g => g.NormalizedName);
        _series = db.Collections.Where(c => c.Kind == CollectionKind.Series).ToDictionary(c => c.NormalizedName);
    }

    public IReadOnlyList<UnreadableFile> Unreadable => _unreadable;

    public void Apply(ScanAction action)
    {
        switch (action)
        {
            case KeepFile:
                break;
            case Reappear(var k):
                Touch(_files[k.FileId]);
                break;
            case MoveFile(var k, var to):
                Refresh(_files[k.FileId], to);
                break;
            case UpdateContent(var k, var now):
                {
                    var file = _files[k.FileId];
                    var book = LoadBook(file.BookId);
                    file.Unreadable = _bookUnreadable[file.BookId] = Describe(book, now);
                    Refresh(file, now);
                    break;
                }
            case SplitToNewBook(var k, var now):
                {
                    var file = _files[k.FileId];
                    var book = new Book();
                    file.Unreadable = Describe(book, now);
                    file.Book = book;
                    Refresh(file, now);
                    _db.Books.Add(book);
                    break;
                }
            case ReassignFile(var k, var bookId, var now):
                {
                    var file = _files[k.FileId];
                    file.BookId = bookId;
                    file.Unreadable = SiblingUnreadable(bookId);
                    Refresh(file, now);
                    break;
                }
            case JoinBook(var bookId, var f):
                _db.BookFiles.Add(NewFile(f, bookId, SiblingUnreadable(bookId)));
                break;
            case AddBook(var files):
                {
                    var book = new Book();
                    var unreadable = Describe(book, files[0]);
                    foreach (var f in files)
                    {
                        var file = NewFile(f, 0, unreadable);
                        file.Book = book;
                        book.Files.Add(file);
                    }

                    _db.Books.Add(book);
                    break;
                }
            case MarkMissing(var k):
                _files[k.FileId].Status = FileStatus.Missing;
                break;
            default:
                throw new NotSupportedException(action.GetType().Name);
        }
    }

    /// <summary>Reads the file and fills the book from it. Returns true when the file could not be read.</summary>
    private bool Describe(Book book, FoundFile file)
    {
        MetadataResult result;
        try
        {
            using var stream = File.OpenRead(file.Path);
            result = BookMetadataReader.Read(stream, file.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Gone or locked since it was hashed: keep the book, titled from the file name.
            result = new MetadataResult(
                new BookMetadata { Title = Path.GetFileNameWithoutExtension(file.Path) }, Unreadable: true, ex.Message);
        }

        if (result.Unreadable)
        {
            _unreadable.Add(new UnreadableFile(file.Path, result.Error ?? "Could not be read."));
        }

        var m = result.Metadata;
        book.Title = m.Title!;
        book.Language = m.Language;
        book.Isbn = m.Isbn;
        book.Series = m.Series is null ? null : Shared(_series, m.Series, (name, key) => new Collection { Name = name, NormalizedName = key, Kind = CollectionKind.Series });
        book.SeriesIndex = m.Series is null ? null : m.SeriesIndex;
        book.CoverFileName = SaveCover(m.Cover, file.ContentHash);

        var authors = Distinct(m.Authors).Select(name => Shared(_authors, name, (n, key) => new Author { Name = n, NormalizedName = key })).ToList();
        book.Authors.RemoveAll(link => !authors.Contains(link.Author));
        for (var i = 0; i < authors.Count; i++)
        {
            var link = book.Authors.FirstOrDefault(l => l.Author == authors[i]);
            if (link is null)
            {
                book.Authors.Add(link = new BookAuthor { Author = authors[i] });
            }

            link.Order = i;
        }

        var genres = Distinct(m.Genres).Select(name => Shared(_genres, name, (n, key) => new Genre { Name = n, NormalizedName = key })).ToList();
        book.Genres.RemoveAll(link => !genres.Contains(link.Genre));
        foreach (var genre in genres.Where(g => book.Genres.All(l => l.Genre != g)))
        {
            book.Genres.Add(new BookGenre { Genre = genre });
        }

        return result.Unreadable;
    }

    private static IEnumerable<string> Distinct(IEnumerable<string> names) =>
        names.DistinctBy(NameNormalizer.Normalize);

    /// <summary>The one entity for this name (matched by normalized key), created on first use.</summary>
    private static T Shared<T>(Dictionary<string, T> cache, string name, Func<string, string, T> create)
    {
        var key = NameNormalizer.Normalize(name);
        if (!cache.TryGetValue(key, out var entity))
        {
            cache[key] = entity = create(name, key);
        }

        return entity;
    }

    private string? SaveCover(CoverImage? cover, string contentHash)
    {
        if (cover is null)
        {
            return null;
        }

        var fileName = contentHash + cover.Extension;
        var path = Path.Combine(_coversFolder, fileName);
        try
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(_coversFolder);
                File.WriteAllBytes(path, cover.Bytes);
            }

            return fileName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // a cover is nice to have; never fail the scan over it
        }
    }

    private Book LoadBook(int bookId) => _db.Books
        .Include(b => b.Authors).ThenInclude(l => l.Author)
        .Include(b => b.Genres).ThenInclude(l => l.Genre)
        .Single(b => b.Id == bookId);

    private bool SiblingUnreadable(int bookId) => _bookUnreadable.GetValueOrDefault(bookId);

    private BookFile NewFile(FoundFile f, int bookId, bool unreadable) => new()
    {
        BookId = bookId,
        Path = f.Path,
        Format = BookMetadataReader.FormatOf(f.Path),
        SizeBytes = f.SizeBytes,
        LastWriteUtc = f.LastWriteUtc,
        ContentHash = f.ContentHash,
        Status = FileStatus.Present,
        Unreadable = unreadable,
        LastSeenUtc = _now,
    };

    private void Refresh(BookFile file, FoundFile f)
    {
        file.Path = f.Path;
        file.SizeBytes = f.SizeBytes;
        file.LastWriteUtc = f.LastWriteUtc;
        file.ContentHash = f.ContentHash;
        Touch(file);
    }

    private void Touch(BookFile file)
    {
        file.Status = FileStatus.Present;
        file.LastSeenUtc = _now;
    }
}
