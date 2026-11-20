namespace ReaderCompanion.Core.Library;

/// <summary>How a fetch went: books looked up, covers found, and lookups that failed (not "no cover").</summary>
public sealed record CoverFetchResult(int Looked, int Found, int Failed);

/// <summary>Gets covers from Open Library for books that have an ISBN and no cover. Only ever runs when asked.</summary>
public interface ICoverFetcher
{
    /// <param name="progress">Reports (done, total).</param>
    Task<CoverFetchResult> FetchMissingAsync(IProgress<(int Done, int Total)>? progress, CancellationToken cancellationToken = default);
}
