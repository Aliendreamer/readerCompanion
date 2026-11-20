using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Goodreads;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.Data;

internal sealed class GoodreadsImport(IDbContextFactory<LibraryDbContext> contexts, IAppPreferences preferences) : IGoodreadsImport
{
    public async Task<GoodreadsSummary> ImportFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(path);
        return await ImportAsync(GoodreadsCsv.Parse(reader), cancellationToken);
    }

    public async Task<GoodreadsSummary> ImportAsync(IReadOnlyList<GoodreadsRow> rows, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var context = NewContext(db);
        var books = await Books(db, context.Shelves).ToListAsync(cancellationToken);
        var byId = books.Where(b => b.GoodreadsId is not null).ToDictionary(b => b.GoodreadsId!);
        var byIsbn = new BookIndex<Book>();
        var byTitle = new BookIndex<Book>();
        foreach (var book in books)
        {
            byIsbn.Add(book, MatchKeys.IsbnKeys(book.Isbn));
            byTitle.Add(book, MatchKeys.TitleKeys(book.Title, FirstAuthor(book)));
        }

        var pending = await db.GoodreadsPending.ToDictionaryAsync(p => p.GoodreadsId, cancellationToken);
        void DropPending(string goodreadsId)
        {
            if (pending.Remove(goodreadsId, out var row))
            {
                db.GoodreadsPending.Remove(row);
            }
        }

        int matched = 0, added = 0, updated = 0, forReview = 0;
        var skipped = new List<string>();
        var seen = new HashSet<string>();
        foreach (var row in rows)
        {
            if (!seen.Add(row.GoodreadsId))
            {
                skipped.Add($"\"{row.Title}\" appears more than once in the export.");
                continue;
            }

            var book = byId.GetValueOrDefault(row.GoodreadsId) ?? byIsbn.Find(MatchKeys.IsbnKeys(row.Isbn));
            if (book is null && byTitle.Find(MatchKeys.TitleKeys(row.Title, row.Authors.FirstOrDefault())) is { } candidate)
            {
                if (MatchKeys.DifferentEditions(row.Isbn, candidate.Isbn))
                {
                    DropPending(row.GoodreadsId); // a newer export replaces the waiting row
                    db.GoodreadsPending.Add(GoodreadsPending.From(row, candidate.Id));
                    forReview++;
                    continue;
                }

                book = candidate;
            }

            DropPending(row.GoodreadsId); // now matched for sure, or no longer a candidate

            if (book is null)
            {
                byId[row.GoodreadsId] = context.AddWanted(row);
                added++;
                continue;
            }

            matched++;
            if (book.GoodreadsId is null && !byId.ContainsKey(row.GoodreadsId))
            {
                book.GoodreadsId = row.GoodreadsId;
                byId[row.GoodreadsId] = book;
            }

            if (context.Apply(book, row))
            {
                updated++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        if (rows.Count > 0) // an empty export does not count as keeping in sync
        {
            preferences.Save(preferences.Current with { GoodreadsLastImportUtc = DateTime.UtcNow });
        }
        return new GoodreadsSummary(matched, added, updated, skipped) { ForReview = forReview };
    }

    public async Task<IReadOnlyList<PendingMatch>> PendingAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var pending = await db.GoodreadsPending.AsNoTracking().OrderBy(p => p.Title).ToListAsync(cancellationToken);
        var ids = pending.Select(p => p.BookId).OfType<int>().ToList();
        var books = await db.Books.AsNoTracking().Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Title, b.Isbn }).ToDictionaryAsync(b => b.Id, cancellationToken);
        return [.. pending.Select(p =>
        {
            var book = p.BookId is { } id ? books.GetValueOrDefault(id) : null;
            return new PendingMatch(p.Id, p.Title, p.AuthorList, p.Isbn, book?.Id, book?.Title, book?.Isbn);
        })];
    }

    public async Task ConfirmAsync(int pendingId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.GoodreadsPending.FindAsync([pendingId], cancellationToken) is not { } pending)
        {
            return;
        }

        var context = NewContext(db);
        var row = pending.ToRow();
        if (pending.BookId is { } bookId && await Books(db, context.Shelves).SingleOrDefaultAsync(b => b.Id == bookId, cancellationToken) is { } book)
        {
            if (book.GoodreadsId is null && !await db.Books.AnyAsync(b => b.GoodreadsId == row.GoodreadsId, cancellationToken))
            {
                book.GoodreadsId = row.GoodreadsId;
            }

            context.Apply(book, row);
        }
        else
        {
            context.AddWanted(row); // the book went away meanwhile
        }

        db.GoodreadsPending.Remove(pending);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(int pendingId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        if (await db.GoodreadsPending.FindAsync([pendingId], cancellationToken) is not { } pending)
        {
            return;
        }

        NewContext(db).AddWanted(pending.ToRow());
        db.GoodreadsPending.Remove(pending);
        await db.SaveChangesAsync(cancellationToken);
    }

    private ImportContext NewContext(LibraryDbContext db) =>
        new(db, new CatalogNames(db), preferences.Current.GoodreadsShelvesAsCollections);

    /// <summary>Books with what an import reads; their collections only when shelves become collections.</summary>
    private static IQueryable<Book> Books(LibraryDbContext db, bool withCollections)
    {
        var books = db.Books.Include(b => b.Sessions).Include(b => b.Authors).ThenInclude(a => a.Author);
        return withCollections ? books.Include(b => b.Collections).ThenInclude(c => c.Collection) : books;
    }

    private static string? FirstAuthor(Book book) => book.Authors.OrderBy(a => a.Order).Select(a => a.Author.Name).FirstOrDefault();

    /// <summary>Applies rows within one context: wanted books, reading data and shelves.</summary>
    private sealed class ImportContext(LibraryDbContext db, CatalogNames names, bool shelves)
    {
        public bool Shelves => shelves;

        public Book AddWanted(GoodreadsRow row)
        {
            var book = new Book
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
            Apply(book, row);
            return book;
        }

        /// <summary>Takes the row's status, rating and read date where the user has not set their own, and its shelves
        /// when asked; true when the book's reading changed.</summary>
        public bool Apply(Book book, GoodreadsRow row)
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

            if (shelves)
            {
                names.AddToCollections(book, row.Shelves);
            }

            return changed;
        }
    }
}
