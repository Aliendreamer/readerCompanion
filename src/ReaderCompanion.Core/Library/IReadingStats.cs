namespace ReaderCompanion.Core.Library;

public interface IReadingStats
{
    /// <summary>The facts the Reading page summarises, one per book (see <see cref="ReadingStats.From"/>).</summary>
    Task<IReadOnlyList<StatsBook>> LoadAsync(CancellationToken cancellationToken = default);
}
