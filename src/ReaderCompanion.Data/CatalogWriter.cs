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
    private readonly CatalogNames _names;
    private readonly BookIndex<int> _wanted = new(); // ids of books without files (from an import), by match keys
    private readonly List<UnreadableFile> _unreadable = [];

    public CatalogWriter(LibraryDbContext db, string coversFolder, DateTime now)
    {
        _db = db;
        _coversFolder = coversFolder;
        _now = now;
        _files = db.BookFiles.ToDictionary(f => f.Id);
        _bookUnreadable = _files.Values.GroupBy(f => f.BookId).ToDictionary(g => g.Key, g => g.First().Unreadable);
        _names = new CatalogNames(db);
        foreach (var w in db.Books.Where(b => !b.Files.Any())
                     .Select(b => new { b.Id, b.Isbn, b.Title, Author = b.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).FirstOrDefault() })
                     .ToList())
        {
            _wanted.Add(w.Id, MatchKeys.For(w.Isbn, w.Title, w.Author));
        }
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
                    var book = new Book { DateAddedUtc = _now };
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
                    var found = new Book { DateAddedUtc = _now };
                    var unreadable = Describe(found, files[0]);

                    // A file for a wanted (not owned) book joins it: the book becomes owned and keeps its history.
                    var wanted = WantedMatch(found);
                    var book = wanted ?? found;
                    if (wanted is null)
                    {
                        _db.Books.Add(book);
                    }
                    else
                    {
                        FillFromFile(wanted, found);
                    }

                    foreach (var f in files)
                    {
                        var file = NewFile(f, 0, unreadable);
                        file.Book = book;
                        book.Files.Add(file);
                    }

                    break;
                }
            case MarkMissing(var k):
                _files[k.FileId].Status = FileStatus.Missing;
                break;
            default:
                throw new NotSupportedException(action.GetType().Name);
        }
    }

    /// <summary>The wanted book with the same ISBN, or title and first author, as the newly read one (each wanted book
    /// is matched at most once per scan).</summary>
    private Book? WantedMatch(Book found)
    {
        var id = _wanted.Find(MatchKeys.For(found.Isbn, found.Title, found.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).FirstOrDefault()));
        if (id == 0)
        {
            return null;
        }

        _wanted.Remove(id);
        return _db.Books
            .Include(b => b.Authors).ThenInclude(l => l.Author)
            .Include(b => b.Genres).ThenInclude(l => l.Genre)
            .Include(b => b.Series)
            .Single(b => b.Id == id);
    }

    /// <summary>A wanted book keeps its own details (from the user or the import) and takes from the file only what it
    /// lacks.</summary>
    private void FillFromFile(Book wanted, Book found)
    {
        wanted.CoverFileName ??= found.CoverFileName;
        wanted.Language ??= found.Language;
        wanted.Isbn ??= found.Isbn;
        if (wanted.Series is null && found.Series is not null)
        {
            wanted.Series = found.Series;
            wanted.SeriesIndex = found.SeriesIndex;
        }

        if (wanted.Authors.Count == 0)
        {
            _names.SetAuthors(wanted, found.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name));
        }

        if (wanted.Genres.Count == 0)
        {
            _names.SetGenres(wanted, found.Genres.Select(g => g.Genre.Name));
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
        var cover = SaveCover(m.Cover, file.ContentHash);
        if (book.EditedUtc is not null)
        {
            // The user's details win over the file's; a new cover is taken, but a missing one never wipes the old.
            book.CoverFileName = cover ?? book.CoverFileName;
            return result.Unreadable;
        }

        book.CoverFileName = cover;

        book.Title = m.Title!;
        book.Language = m.Language;
        book.Isbn = m.Isbn;
        book.Series = _names.Series(m.Series);
        book.SeriesIndex = book.Series is null ? null : m.SeriesIndex;

        _names.SetAuthors(book, m.Authors);
        _names.SetGenres(book, m.Genres);

        return result.Unreadable;
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
