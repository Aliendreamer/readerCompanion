using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Goodreads;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

internal sealed class GoodreadsImport(IDbContextFactory<LibraryDbContext> contexts) : IGoodreadsImport
{
    public async Task<GoodreadsSummary> ImportFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(path);
        return await ImportAsync(GoodreadsCsv.Parse(reader), cancellationToken);
    }

    public async Task<GoodreadsSummary> ImportAsync(IReadOnlyList<GoodreadsRow> rows, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var books = await db.Books
            .Include(b => b.Sessions)
            .Include(b => b.Authors).ThenInclude(a => a.Author)
            .ToListAsync(cancellationToken);
        var byId = books.Where(b => b.GoodreadsId is not null).ToDictionary(b => b.GoodreadsId!);
        var byKeys = new BookIndex<Book>();
        foreach (var book in books)
        {
            byKeys.Add(book, MatchKeys.For(book.Isbn, book.Title, book.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).FirstOrDefault()));
        }

        var names = new CatalogNames(db);

        int matched = 0, added = 0, updated = 0;
        var skipped = new List<string>();
        var seen = new HashSet<string>();
        foreach (var row in rows)
        {
            if (!seen.Add(row.GoodreadsId))
            {
                skipped.Add($"\"{row.Title}\" appears more than once in the export.");
                continue;
            }

            var book = byId.GetValueOrDefault(row.GoodreadsId) ?? byKeys.Find(MatchKeys.For(row.Isbn, row.Title, row.Authors.FirstOrDefault()));
            if (book is null)
            {
                book = new Book
                {
                    Title = row.Title,
                    Isbn = row.Isbn,
                    GoodreadsId = row.GoodreadsId,
                    DateAddedUtc = (row.DateAdded ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                };
                names.SetAuthors(book, row.Authors);
                book.Series = names.Series(row.Series);
                book.SeriesIndex = book.Series is null ? null : row.SeriesIndex;
                db.Books.Add(book);
                byId[row.GoodreadsId] = book;
                Apply(book, row);
                added++;
                continue;
            }

            matched++;
            if (book.GoodreadsId is null && !byId.ContainsKey(row.GoodreadsId))
            {
                book.GoodreadsId = row.GoodreadsId;
                byId[row.GoodreadsId] = book;
            }

            if (Apply(book, row))
            {
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new GoodreadsSummary(matched, added, updated, skipped);
    }

    /// <summary>Takes the row's status, rating and read date where the user has not set their own; true when anything
    /// changed.</summary>
    private static bool Apply(Book book, GoodreadsRow row)
    {
        var changed = false;
        if (row.Status is { } status && book.ReadStatus != status
            && (book.ReadStatus == ReadStatus.NotStarted || book.ReadStatus == book.GoodreadsStatus))
        {
            book.ReadStatus = status;
            changed = true;
        }

        if (row.Status is not null && book.ReadStatus == row.Status)
        {
            book.GoodreadsStatus = row.Status; // in step with Goodreads: future imports may change it
        }

        if (row.Rating is { } rating && book.Rating != rating && (book.Rating is null || book.Rating == book.GoodreadsRating))
        {
            book.Rating = rating;
            changed = true;
        }

        if (row.Rating is not null && book.Rating == row.Rating)
        {
            book.GoodreadsRating = row.Rating;
        }

        // A read the user logged within a few days is the same read, not a second one.
        if (row.DateRead is { } read && book.Sessions.All(s => s.FinishedOn is not { } done || Math.Abs(done.DayNumber - read.DayNumber) > 3))
        {
            book.Sessions.Add(new ReadingSession { FinishedOn = read, FromGoodreads = true });
            changed = true;
        }

        return changed;
    }
}
