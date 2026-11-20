namespace ReaderCompanion.Core.Library;

/// <summary>One reading session as shown and edited by the user.</summary>
public sealed record SessionInfo(int Id, DateOnly? StartedOn, DateOnly? FinishedOn, bool Abandoned);

/// <summary>A book's reading data; <see cref="Sessions"/> are newest first.</summary>
public sealed record ReadingInfo(ReadStatus Status, int? Rating, string? Notes, IReadOnlyList<SessionInfo> Sessions);

/// <summary>The user's reading of each book. Scans never change it. Methods return false (or null) for a book or
/// session that does not exist.</summary>
public interface IReading
{
    Task<ReadingInfo?> GetAsync(int bookId, CancellationToken cancellationToken = default);

    /// <summary>Sets the status and keeps the sessions in step (see <see cref="ReadingLog"/>); setting the status the
    /// book already has changes nothing.</summary>
    Task<bool> SetStatusAsync(int bookId, ReadStatus status, DateOnly today, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentOutOfRangeException">The rating is not 1 to 5 (null clears it).</exception>
    Task<bool> SetRatingAsync(int bookId, int? rating, CancellationToken cancellationToken = default);

    /// <summary>A blank note clears it.</summary>
    Task<bool> SetNotesAsync(int bookId, string? notes, CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The finish date is before the start date.</exception>
    Task<bool> UpdateSessionAsync(int sessionId, DateOnly? startedOn, DateOnly? finishedOn, CancellationToken cancellationToken = default);

    Task<bool> DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default);
}
