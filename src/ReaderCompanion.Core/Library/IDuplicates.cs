namespace ReaderCompanion.Core.Library;

/// <summary>Books that look like the same book, ordered as <see cref="DuplicateFinder"/> orders them.</summary>
public sealed record DuplicateGroup(IReadOnlyList<BookDetails> Books)
{
    /// <summary>Identifies the group on screen: its first book's id.</summary>
    public int Key => Books[0].Book.Id;
}

public interface IDuplicates
{
    /// <summary>Groups of likely duplicates, leaving out pairs the user dismissed.</summary>
    Task<IReadOnlyList<DuplicateGroup>> FindAsync(CancellationToken cancellationToken = default);

    /// <summary>Keeps <paramref name="keepId"/>'s details, moves every file of <paramref name="otherIds"/> to it,
    /// adds their genres, keeps the earliest date added and a cover if it has none, carries their "not duplicates"
    /// answers over to it, marks it edited (the user chose its details), and removes the other books.
    /// Returns false if any of the books no longer exists (then nothing changes).</summary>
    Task<bool> MergeAsync(int keepId, IReadOnlyCollection<int> otherIds, CancellationToken cancellationToken = default);

    /// <summary>Remembers that these books are not duplicates of each other; books that no longer exist are
    /// skipped.</summary>
    Task DismissAsync(IReadOnlyCollection<int> bookIds, CancellationToken cancellationToken = default);
}
