using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>The Library's filter tree, in the pane on the left.</summary>
public class FilterTreeTests : BunitContext
{
    public FilterTreeTests() => Services.AddAppFakes(
    [
        new(1, "Dune", ["Frank Herbert"], null, null, false) { Genres = ["Science Fiction"], Formats = [BookFormat.Epub] },
        new(2, "Mort", ["Terry Pratchett"], "Discworld", 4, false) { Genres = ["Fantasy"], Formats = [BookFormat.Pdf] },
    ]);

    private IRenderedComponent<Home> RenderHome()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));
        return cut;
    }

    private static IReadOnlyList<string> Titles(IRenderedComponent<Home> cut) => [.. cut.FindAll(".book-title").Select(t => t.TextContent)];

    [Fact]
    public void Sections_InOrder_LongOnesStartClosed()
    {
        var cut = RenderHome();

        var headers = cut.FindAll(".workspace-tree .filter-section-header");
        Assert.Equal(
            ["Genres", "Authors", "Collections", "Formats", "Status", "Owned / wanted"],
            headers.Select(h => h.QuerySelector(".filter-section-name")!.TextContent));
        Assert.Equal(["true", "false", "false", "true", "true", "true"], headers.Select(h => h.GetAttribute("aria-expanded")));
        Assert.Contains("(2)", cut.Find("#filter-genre").TextContent); // how many values the section has
        Assert.Single(cut.FindAll(".workspace-tree #browse-missing"));
    }

    [Fact]
    public void TickingNarrows_AndClearFiltersAppears()
    {
        var cut = RenderHome();
        Assert.Empty(cut.FindAll("#clear-filters"));

        FilterSteps.Pick(cut, "filter-genre", "Science Fiction");

        Assert.Equal(["Dune"], Titles(cut));
        cut.Find(".workspace-tree #clear-filters").Click();
        Assert.Equal(2, Titles(cut).Count);
    }

    [Fact]
    public void OpenSections_AreRemembered_WhenComingBack()
    {
        var first = RenderHome();
        first.Find("#filter-author").Click();
        first.Dispose();

        var back = RenderHome();

        Assert.Equal("true", back.Find("#filter-author").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void NarrowWindow_FiltersButtonShowsTheTree()
    {
        var cut = RenderHome();
        Assert.DoesNotContain("tree-open", cut.Find(".workspace").ClassList);

        cut.Find("#filters-toggle").Click();

        Assert.Contains("tree-open", cut.Find(".workspace").ClassList);
    }
}
