using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class ReadingStatsTests : IDisposable
{
    private readonly TempLibrary _library = new();

    [Fact]
    public async Task Load_GivesEachBooksFacts_WithOnlyFinishedReadsThatWereNotAbandoned()
    {
        using (var db = _library.NewContext())
        {
            var names = new CatalogNames(db);
            var mort = new Book { Title = "Mort", ReadStatus = ReadStatus.Read, Series = names.Series("Discworld") };
            names.SetAuthors(mort, ["Terry Pratchett"]);
            mort.Files.Add(TestFiles.File("/b/mort.epub"));
            mort.Sessions.Add(new ReadingSession { FinishedOn = new DateOnly(2026, 1, 10) });
            mort.Sessions.Add(new ReadingSession { FinishedOn = new DateOnly(2025, 1, 10), Abandoned = true });
            mort.Sessions.Add(new ReadingSession { StartedOn = new DateOnly(2026, 3, 1) });
            db.Books.AddRange(mort, new Book { Title = "Wanted" });
            db.SaveChanges();
        }

        var books = await _library.Services.CreateScope().ServiceProvider.GetRequiredService<IReadingStats>()
            .LoadAsync(TestContext.Current.CancellationToken);

        var mortFacts = books.Single(b => b.Title == "Mort");
        Assert.Equal(["Terry Pratchett"], mortFacts.Authors);
        Assert.Equal("Discworld", mortFacts.Series);
        Assert.True(mortFacts.Owned);
        Assert.Equal(ReadStatus.Read, mortFacts.Status);
        Assert.Equal([new DateOnly(2026, 1, 10)], mortFacts.FinishedReads);
        Assert.False(books.Single(b => b.Title == "Wanted").Owned);
    }

    public void Dispose() => _library.Dispose();
}
