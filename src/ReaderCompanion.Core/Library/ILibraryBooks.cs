namespace ReaderCompanion.Core.Library;

/// <summary>A book as shown in a list. <see cref="FileMissing"/> is true when none of its files is present.</summary>
public sealed record BookListItem(
    int Id, string Title, IReadOnlyList<string> Authors, string? Series, decimal? SeriesIndex, bool FileMissing);

public interface ILibraryBooks
{
    /// <summary>All books, ordered by title.</summary>
    Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default);
}
