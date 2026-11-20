using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

internal sealed class Duplicates(IDbContextFactory<LibraryDbContext> contexts, ILibraryBooks books) : IDuplicates
{
    public async Task<IReadOnlyList<DuplicateGroup>> FindAsync(CancellationToken cancellationToken = default)
    {
        List<DuplicateCandidate> candidates;
        HashSet<(int, int)> dismissed;
        await using (var db = await contexts.CreateDbContextAsync(cancellationToken))
        {
            candidates = await db.Books.AsNoTracking()
                .Select(b => new DuplicateCandidate(
                    b.Id,
                    b.Title,
                    b.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).FirstOrDefault(),
                    b.Isbn))
                .ToListAsync(cancellationToken);
            dismissed = await DismissedPairs(db.DismissedDuplicates.AsNoTracking(), cancellationToken);
        }

        var groups = DuplicateFinder.Find(candidates, dismissed);
        var details = (await books.GetManyAsync([.. groups.SelectMany(g => g)], cancellationToken))
            .ToDictionary(d => d.Book.Id);
        return [.. groups
            .Select(ids => ids.Where(details.ContainsKey).Select(id => details[id]).ToList())
            .Where(g => g.Count > 1) // a book removed meanwhile
            .Select(g => new DuplicateGroup(g))];
    }

    public async Task<bool> MergeAsync(int keepId, IReadOnlyCollection<int> otherIds, CancellationToken cancellationToken = default)
    {
        var others = otherIds.Where(id => id != keepId).Distinct().ToList();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var loaded = await db.Books
            .Include(b => b.Files)
            .Include(b => b.Genres)
            .Include(b => b.Collections)
            .Include(b => b.Sessions)
            .Where(b => b.Id == keepId || others.Contains(b.Id))
            .ToListAsync(cancellationToken);
        if (loaded.Count != others.Count + 1)
        {
            return false;
        }

        var keep = loaded.Single(b => b.Id == keepId);
        var extraOpenReads = ReadingLog.CombineOpen(loaded.SelectMany(b => b.Sessions));
        db.ReadingSessions.RemoveRange(extraOpenReads);
        keep.EditedUtc = DateTime.UtcNow; // the user chose these details; scans keep them
        foreach (var other in loaded.Where(b => b.Id != keepId))
        {
            foreach (var file in other.Files.ToList())
            {
                file.Book = keep;
            }

            foreach (var genreId in other.Genres.Select(g => g.GenreId).Except(keep.Genres.Select(g => g.GenreId)))
            {
                keep.Genres.Add(new BookGenre { GenreId = genreId });
            }

            foreach (var collectionId in other.Collections.Select(c => c.CollectionId).Except(keep.Collections.Select(c => c.CollectionId)))
            {
                keep.Collections.Add(new BookCollection { CollectionId = collectionId });
            }

            foreach (var session in other.Sessions.Except(extraOpenReads).ToList())
            {
                session.BookId = keepId; // reading history is kept whole
            }

            if (keep.ReadStatus == ReadStatus.NotStarted)
            {
                keep.ReadStatus = other.ReadStatus;
            }

            keep.Rating ??= other.Rating;
            keep.Notes = keep.Notes is null || other.Notes is null ? keep.Notes ?? other.Notes : $"{keep.Notes}\n\n{other.Notes}";

            if (other.DateAddedUtc < keep.DateAddedUtc)
            {
                keep.DateAddedUtc = other.DateAddedUtc;
            }

            keep.CoverFileName ??= other.CoverFileName;
            db.Books.Remove(other); // its author and genre links and its own dismissals go with it
        }

        // An open read (from either book) means the merged book is being read.
        if (loaded.SelectMany(b => b.Sessions).Except(extraOpenReads).Any(s => s.FinishedOn is null))
        {
            keep.ReadStatus = ReadStatus.Reading;
        }

        // "Not duplicates" answers about the removed books now apply to the kept one.
        var dismissals = db.DismissedDuplicates.Where(p => others.Contains(p.BookIdA) || others.Contains(p.BookIdB));
        var carried = (await dismissals.ToListAsync(cancellationToken))
            .Select(p => others.Contains(p.BookIdA) ? p.BookIdB : p.BookIdA)
            .Where(id => id != keepId && !others.Contains(id));
        await AddPairsAsync(db, carried.SelectMany(id => DuplicateFinder.Pairs([keepId, id])), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DismissAsync(IReadOnlyCollection<int> bookIds, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var existing = await db.Books.Where(b => bookIds.Contains(b.Id)).Select(b => b.Id).ToListAsync(cancellationToken);
        await AddPairsAsync(db, DuplicateFinder.Pairs(existing), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Adds the dismissals not already stored.</summary>
    private static async Task AddPairsAsync(LibraryDbContext db, IEnumerable<(int, int)> pairs, CancellationToken cancellationToken)
    {
        var existing = await DismissedPairs(db.DismissedDuplicates, cancellationToken);
        foreach (var (a, b) in pairs.Where(existing.Add))
        {
            db.DismissedDuplicates.Add(new DismissedDuplicate { BookIdA = a, BookIdB = b });
        }
    }

    private static async Task<HashSet<(int, int)>> DismissedPairs(IQueryable<DismissedDuplicate> query, CancellationToken cancellationToken) =>
        (await query.ToListAsync(cancellationToken)).Select(p => (p.BookIdA, p.BookIdB)).ToHashSet();
}
