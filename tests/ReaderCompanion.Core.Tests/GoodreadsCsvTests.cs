using ReaderCompanion.Core.Goodreads;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

public class GoodreadsCsvTests
{
    private const string Header =
        "Book Id,Title,Author,Author l-f,Additional Authors,ISBN,ISBN13,My Rating,Average Rating,Publisher,Binding," +
        "Number of Pages,Year Published,Original Publication Year,Date Read,Date Added,Bookshelves," +
        "Bookshelves with positions,Exclusive Shelf,My Review,Spoiler,Private Notes,Read Count,Owned Copies";

    private static IReadOnlyList<GoodreadsRow> Parse(params string[] rows) =>
        GoodreadsCsv.Parse(new StringReader(string.Join("\n", [Header, .. rows])));

    [Fact]
    public void ReadsARow_WithGoodreadsIsbnQuoting_DatesRatingAndShelf()
    {
        var row = Assert.Single(Parse(
            "386162,Mort,Terry Pratchett,\"Pratchett, Terry\",,\"=\"\"0552131067\"\"\",\"=\"\"9780552131063\"\"\",4,4.24,Corgi," +
            "Paperback,272,1988,1987,2024/03/02,2023/12/25,\"discworld, read, favourites\",\"discworld (#3)\",read,,,,1,0"));

        Assert.Equal("386162", row.GoodreadsId);
        Assert.Equal("Mort", row.Title);
        Assert.Equal(["Terry Pratchett"], row.Authors);
        Assert.Equal("9780552131063", row.Isbn);
        Assert.Equal(4, row.Rating);
        Assert.Equal(new DateOnly(2024, 3, 2), row.DateRead);
        Assert.Equal(new DateOnly(2023, 12, 25), row.DateAdded);
        Assert.Equal(ReadStatus.Read, row.Status);
        Assert.Equal(["discworld", "favourites"], row.Shelves);
    }

    [Theory]
    [InlineData("to-read", ReadStatus.WantToRead)]
    [InlineData("currently-reading", ReadStatus.Reading)]
    [InlineData("read", ReadStatus.Read)]
    [InlineData("did-not-finish", null)]
    public void ExclusiveShelf_MapsToStatus(string shelf, ReadStatus? status)
    {
        Assert.Equal(status, Assert.Single(Parse($"1,T,A,,,,,0,0,,,,,,,,,,{shelf},,,,0,0")).Status);
    }

    [Fact]
    public void QuotedFields_KeepCommasQuotesAndNewlines_AndAdditionalAuthorsAreSplit()
    {
        var row = Assert.Single(Parse(
            "7,\"Good Omens: The \"\"Nice\"\" and\nAccurate Prophecies\",Terry Pratchett,,\"Neil Gaiman, Someone Else\",=\"\",=\"\",0,0,,,,,,,,,,to-read,,,,0,0"));

        Assert.Equal("Good Omens: The \"Nice\" and Accurate Prophecies", row.Title); // the newline is kept by the CSV, then cleaned like any name
        Assert.Equal(["Terry Pratchett", "Neil Gaiman", "Someone Else"], row.Authors);
        Assert.Null(row.Isbn);
        Assert.Null(row.Rating);
        Assert.Null(row.DateRead);
    }

    [Theory]
    [InlineData("Mistborn (Mistborn, #1)", "Mistborn", "Mistborn", 1.0)]
    [InlineData("The Light Fantastic (Discworld, #2)", "The Light Fantastic", "Discworld", 2.0)]
    [InlineData("Interesting Times (Discworld #17.5)", "Interesting Times", "Discworld", 17.5)]
    [InlineData("Gödel, Escher, Bach (20th Anniversary Edition)", "Gödel, Escher, Bach (20th Anniversary Edition)", null, null)]
    public void SeriesSuffix_IsSplitOffTheTitle(string raw, string title, string? series, double? index)
    {
        var row = Assert.Single(Parse($"1,\"{raw}\",A,,,,,0,0,,,,,,,,,,read,,,,0,0"));

        Assert.Equal((title, series, (decimal?)index), (row.Title, row.Series, row.SeriesIndex));
    }

    [Fact]
    public void Isbn10_IsUsed_WhenThereIsNoIsbn13()
    {
        Assert.Equal("0552131067", Assert.Single(Parse("1,T,A,,,=\"0552131067\",=\"\",0,0,,,,,,,,,,read,,,,0,0")).Isbn);
    }

    [Fact]
    public void NotAnExport_IsRefused()
    {
        Assert.Throws<InvalidDataException>(() => GoodreadsCsv.Parse(new StringReader("name,price\napple,1")));
        Assert.Throws<InvalidDataException>(() => GoodreadsCsv.Parse(new StringReader("")));
    }
}
