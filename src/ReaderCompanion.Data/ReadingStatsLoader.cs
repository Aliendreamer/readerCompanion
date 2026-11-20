using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

internal sealed class ReadingStatsLoader(IDbContextFactory<LibraryDbContext> contexts) : IReadingStats
{
    public async Task<IReadOnlyList<StatsBook>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.Books.AsNoTracking()
            .Select(b => new StatsBook(
                b.Title,
                b.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).ToList(),
                b.Series != null ? b.Series.Name : null,
                b.Files.Any(),
                b.ReadStatus,
                b.Sessions.AsQueryable().Where(ReadingLog.IsRead).Select(s => s.FinishedOn!.Value).ToList()))
            .ToListAsync(cancellationToken);
    }
}
