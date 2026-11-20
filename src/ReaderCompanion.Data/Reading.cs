using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

internal sealed class Reading(IDbContextFactory<LibraryDbContext> contexts) : IReading
{
    public async Task<ReadingInfo?> GetAsync(int bookId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.Books.AsNoTracking()
            .Where(b => b.Id == bookId)
            .Select(b => new ReadingInfo(
                b.ReadStatus,
                b.Rating,
                b.Notes,
                b.Sessions
                    .OrderByDescending(s => s.FinishedOn == null)
                    .ThenByDescending(s => s.FinishedOn ?? s.StartedOn)
                    .ThenByDescending(s => s.Id)
                    .Select(s => new SessionInfo(s.Id, s.StartedOn, s.FinishedOn, s.Abandoned))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> SetStatusAsync(int bookId, ReadStatus status, DateOnly today, CancellationToken cancellationToken = default) =>
        ChangeBookAsync(bookId, book =>
        {
            if (book.ReadStatus != status) // pressing "Read" twice records one read, not two
            {
                book.ReadStatus = status;
                ReadingLog.ChangeStatus(book.Sessions, status, today);
            }
        }, cancellationToken);

    public Task<bool> SetRatingAsync(int bookId, int? rating, CancellationToken cancellationToken = default)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "A rating is 1 to 5 stars.");
        }

        return ChangeBookAsync(bookId, book => book.Rating = rating, cancellationToken);
    }

    public Task<bool> SetNotesAsync(int bookId, string? notes, CancellationToken cancellationToken = default) =>
        ChangeBookAsync(bookId, book => book.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(), cancellationToken);

    public async Task<bool> UpdateSessionAsync(int sessionId, DateOnly? startedOn, DateOnly? finishedOn, CancellationToken cancellationToken = default)
    {
        if (ReadingLog.DateError(startedOn, finishedOn) is { } error)
        {
            throw new ArgumentException(error, nameof(finishedOn));
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.ReadingSessions.FindAsync([sessionId], cancellationToken) is not { } session)
        {
            return false;
        }

        session.StartedOn = startedOn;
        session.FinishedOn = finishedOn;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.ReadingSessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(cancellationToken) > 0;
    }

    private async Task<bool> ChangeBookAsync(int bookId, Action<Book> change, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.Books.Include(b => b.Sessions).SingleOrDefaultAsync(b => b.Id == bookId, cancellationToken) is not { } book)
        {
            return false;
        }

        change(book);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
