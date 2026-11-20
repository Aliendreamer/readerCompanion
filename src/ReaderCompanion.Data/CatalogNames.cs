using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Data;

/// <summary>The shared authors, genres and series of a tracked catalog, matched by normalized name and created on
/// first use. Scans and the user's edits both attach names through here, so they match the same way.</summary>
internal sealed class CatalogNames(LibraryDbContext db)
{
    private readonly Dictionary<string, Author> _authors = db.Authors.ToDictionary(a => a.NormalizedName);
    private readonly Dictionary<string, Genre> _genres = db.Genres.ToDictionary(g => g.NormalizedName);
    private readonly Dictionary<string, Collection> _series = db.Collections
        .Where(c => c.Kind == CollectionKind.Series)
        .ToDictionary(c => c.NormalizedName);

    /// <summary>The series called <paramref name="name"/>, or null for no series.</summary>
    public Collection? Series(string? name) => NameNormalizer.Clean(name) is { } clean
        ? Shared(_series, clean, (n, key) => new Collection { Name = n, NormalizedName = key, Kind = CollectionKind.Series })
        : null;

    private Dictionary<string, Collection>? _userCollections;

    /// <summary>Puts the book into the user collections called <paramref name="names"/> (created on first use); never
    /// takes it out of any.</summary>
    public void AddToCollections(Book book, IEnumerable<string> names)
    {
        _userCollections ??= db.Collections.Where(c => c.Kind == CollectionKind.User).ToDictionary(c => c.NormalizedName);
        foreach (var name in Clean(names))
        {
            var collection = Shared(_userCollections, name, (n, key) => new Collection { Name = n, NormalizedName = key, Kind = CollectionKind.User });
            if (book.Collections.All(l => l.Collection != collection))
            {
                book.Collections.Add(new BookCollection { Collection = collection });
            }
        }
    }

    /// <summary>Makes the book's authors exactly <paramref name="names"/>, in that order.</summary>
    public void SetAuthors(Book book, IEnumerable<string> names)
    {
        var authors = Clean(names).Select(n => Shared(_authors, n, (name, key) => new Author { Name = name, NormalizedName = key })).ToList();
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
    }

    /// <summary>Makes the book's genres exactly <paramref name="names"/>.</summary>
    public void SetGenres(Book book, IEnumerable<string> names)
    {
        var genres = Clean(names).Select(n => Shared(_genres, n, (name, key) => new Genre { Name = name, NormalizedName = key })).ToList();
        book.Genres.RemoveAll(link => !genres.Contains(link.Genre));
        foreach (var genre in genres.Where(g => book.Genres.All(l => l.Genre != g)))
        {
            book.Genres.Add(new BookGenre { Genre = genre });
        }
    }

    /// <summary>Cleaned names, without blanks or repeats (compared by normalized key).</summary>
    private static IEnumerable<string> Clean(IEnumerable<string> names) =>
        names.Select(NameNormalizer.Clean).OfType<string>().DistinctBy(NameNormalizer.Normalize);

    private static T Shared<T>(Dictionary<string, T> cache, string name, Func<string, string, T> create)
    {
        var key = NameNormalizer.Normalize(name);
        if (!cache.TryGetValue(key, out var entity))
        {
            cache[key] = entity = create(name, key);
        }

        return entity;
    }
}
