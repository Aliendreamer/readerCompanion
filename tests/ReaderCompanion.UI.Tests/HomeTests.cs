using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class HomeTests : BunitContext
{
    private readonly List<BookListItem> _books = [];

    public HomeTests()
    {
        Services.AddSingleton<ILibraryBooks>(new FakeBooks(_books));
        Services.AddSingleton<ILibraryFolders>(new FakeFolders());
        Services.AddSingleton<ILibraryScanner>(new FakeScanner());
        Services.AddSingleton<IFolderPicker>(new FakePicker());
    }

    [Fact]
    public void EmptyLibrary_ShowsTitleAndEmptyMessage()
    {
        var cut = Render<Home>();

        Assert.Equal("readerCompanion", cut.Find("h1").TextContent);
        cut.WaitForAssertion(() => Assert.Contains("Your library is empty.", cut.Markup));
    }

    [Fact]
    public void Routes_AtRoot_RendersHomeInsideLayout()
    {
        var cut = Render<Routes>();

        Assert.Equal("readerCompanion", cut.Find("main h1").TextContent);
    }

    [Fact]
    public void Book_ShowsTitleAndAuthorsInOrder()
    {
        _books.Add(new BookListItem(1, "Good Omens", ["Terry Pratchett", "Neil Gaiman"], null, null, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("Good Omens", cut.Find(".book-title").TextContent));
        Assert.Equal("Terry Pratchett, Neil Gaiman", cut.Find(".book-authors").TextContent);
        Assert.DoesNotContain("Your library is empty.", cut.Markup);
    }

    [Fact]
    public void BookWithoutAuthors_ShowsUnknownAuthor()
    {
        _books.Add(new BookListItem(1, "Mystery", [], null, null, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal("Unknown author", cut.Find(".book-authors").TextContent));
    }

    [Theory]
    [InlineData(1.0, "Discworld #1")]
    [InlineData(2.5, "Discworld #2.5")]
    [InlineData(null, "Discworld")]
    public void BookInSeries_ShowsSeriesAndNumber(double? index, string expected)
    {
        _books.Add(new BookListItem(1, "Book", ["A"], "Discworld", (decimal?)index, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Equal(expected, cut.Find(".book-series").TextContent));
    }

    [Fact]
    public void BookWithMissingFile_IsMarked()
    {
        _books.Add(new BookListItem(1, "Gone", ["A"], null, null, true));
        _books.Add(new BookListItem(2, "Here", ["A"], null, null, false));

        var cut = Render<Home>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".book-missing")));
        Assert.Contains("Gone", cut.Find(".book-missing").Closest("li")!.TextContent);
    }

    private sealed class FakeBooks(List<BookListItem> books) : ILibraryBooks
    {
        public Task<IReadOnlyList<BookListItem>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BookListItem>>([.. books]);
    }
}
