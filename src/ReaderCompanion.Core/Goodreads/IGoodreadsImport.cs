namespace ReaderCompanion.Core.Goodreads;

/// <summary>What an import did. <see cref="Matched"/> rows found an existing book (owned or wanted);
/// <see cref="Updated"/> counts those whose reading changed.</summary>
public sealed record GoodreadsSummary(int Matched, int AddedWanted, int Updated, IReadOnlyList<string> Skipped);

/// <summary>Brings a Goodreads library export into the library. Safe to repeat: it never duplicates books or reads,
/// and never overwrites a status or rating the user changed in the app.</summary>
public interface IGoodreadsImport
{
    Task<GoodreadsSummary> ImportAsync(IReadOnlyList<GoodreadsRow> rows, CancellationToken cancellationToken = default);

    /// <summary>Reads the export file, then imports it.</summary>
    /// <exception cref="InvalidDataException">The file is not a Goodreads library export; nothing changed.</exception>
    Task<GoodreadsSummary> ImportFileAsync(string path, CancellationToken cancellationToken = default);
}
