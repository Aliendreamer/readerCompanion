using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Preferences;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>A library of thousands of books shows the first ones and adds more while scrolling.</summary>
public class LargeLibraryTests : BunitContext
{
    private readonly FakeInView _inView = new();
    private readonly FakePreferences _preferences = new();

    public LargeLibraryTests()
    {
        List<BookListItem> books = [.. Enumerable.Range(1, 300).Select(i =>
            new BookListItem(i, $"Book {i:000}", [i % 2 == 0 ? "Even Author" : "Odd Author"], null, null, false))];
        Services.AddAppFakes(books, _preferences, inView: _inView);
    }

    private IRenderedComponent<Home> RenderHome()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-card")));
        return cut;
    }

    [Fact]
    public void FirstOnlySome_ThenMoreNearTheEnd_OrOnShowMore()
    {
        var cut = RenderHome();
        Assert.Equal(120, cut.FindAll(".book-card").Count);
        Assert.Equal("300 books", cut.Find(".book-count").TextContent);
        Assert.Contains("Showing 120 of 300", cut.Find(".load-more").TextContent);

        cut.InvokeAsync(_inView.ScrollNear);
        cut.WaitForAssertion(() => Assert.Equal(240, cut.FindAll(".book-card").Count));

        cut.Find("#show-more").Click();
        Assert.Equal(300, cut.FindAll(".book-card").Count);
        Assert.Empty(cut.FindAll(".load-more"));
    }

    [Fact]
    public void ANewSearch_StartsAgainFromTheTop()
    {
        var cut = RenderHome();
        cut.Find("#show-more").Click();
        Assert.Equal(240, cut.FindAll(".book-card").Count);

        cut.Find("#browse-search").Input("book");

        Assert.Equal(120, cut.FindAll(".book-card").Count);
    }

    [Fact]
    public void Groups_KeepTheirFullCounts_WhileOnlyTheFirstBooksAreShown()
    {
        _preferences.Save(_preferences.Current with { LibraryGroup = LibraryGroup.Author });
        var cut = RenderHome();

        Assert.Equal(["(150)"], cut.FindAll(".section-count").Select(c => c.TextContent).Distinct());
        Assert.Equal(120, cut.FindAll(".book-card").Count);
    }
}
