namespace ReaderCompanion.Core.Library;

/// <summary>Finds books by their <see cref="MatchKeys.For"/> keys, trying them in order. Several books may share a
/// key; the earliest added that is still in the index is found.</summary>
public sealed class BookIndex<T>
    where T : notnull
{
    private readonly Dictionary<string, List<T>> _byKey = [];
    private readonly Dictionary<T, IReadOnlyList<string>> _keysOf = [];

    public void Add(T book, IReadOnlyList<string> keys)
    {
        _keysOf[book] = keys;
        foreach (var key in keys)
        {
            (_byKey.TryGetValue(key, out var books) ? books : _byKey[key] = []).Add(book);
        }
    }

    /// <summary>The book under the first key that has one, or <c>default</c>.</summary>
    public T? Find(IReadOnlyList<string> keys)
    {
        foreach (var key in keys)
        {
            if (_byKey.TryGetValue(key, out var books) && books.Count > 0)
            {
                return books[0];
            }
        }

        return default;
    }

    /// <summary>Forgets the book, so it is not found again.</summary>
    public void Remove(T book)
    {
        if (_keysOf.Remove(book, out var keys))
        {
            foreach (var key in keys)
            {
                _byKey[key].Remove(book);
            }
        }
    }
}
