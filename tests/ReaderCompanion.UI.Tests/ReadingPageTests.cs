using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class ReadingPageTests : BunitContext
{
    private static readonly int Year = DateTime.Now.Year;
    private readonly FakeReadingStats _stats = new();

    public ReadingPageTests() => Services.AddAppFakes(stats: _stats);

    private IRenderedComponent<ReadingPage> RenderPage()
    {
        var cut = Render<ReadingPage>();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loading")));
        return cut;
    }

    [Fact]
    public void EmptyLibrary_SaysThereIsNothingYet()
    {
        Assert.Contains("Nothing to show yet", RenderPage().Markup);
    }

    [Fact]
    public void LoadFailure_IsShown()
    {
        _stats.Fail = true;

        Assert.Contains("could not be loaded", RenderPage().Find(".panel-error").TextContent);
    }

    [Fact]
    public void ShowsGlance_ThisYear_ByYear_AuthorsAndSeries()
    {
        _stats.Books.AddRange(
        [
            new("Mort", ["Terry Pratchett"], "Discworld", true, ReadStatus.Read, [new(Year, 1, 10), new(Year, 3, 2)]),
            new("Reaper Man", ["Terry Pratchett"], "Discworld", true, ReadStatus.NotStarted, []),
            new("Dune", ["Frank Herbert"], null, true, ReadStatus.Read, [new(Year - 1, 6, 1)]),
        ]);

        var cut = RenderPage();

        Assert.Equal(["2", "1", "0", "0"], cut.FindAll(".glance .glance-count").Select(e => e.TextContent));
        Assert.Contains("2 reads", cut.Find(".this-year").TextContent);
        Assert.Contains("Mort ×2", cut.Find(".this-year").TextContent);
        Assert.Equal([$"{Year}: 2", $"{Year - 1}: 1"], cut.FindAll(".by-year li").Select(e => e.TextContent.Trim()));
        Assert.Contains("1 of 2 read", cut.Find(".series-progress").TextContent);
        // Both have one book read; ties go by name.
        Assert.Equal(["Frank Herbert", "Terry Pratchett"], cut.FindAll(".author-progress tbody tr td:first-child").Select(e => e.TextContent));
    }

    [Fact]
    public void ASeries_Opens_ToShowItsBooks()
    {
        _stats.Books.AddRange(
        [
            new("Reaper Man", ["Terry Pratchett"], "Discworld", true, ReadStatus.NotStarted, []) { SeriesIndex = 11 },
            new("Mort", ["Terry Pratchett"], "Discworld", true, ReadStatus.Read, [new(Year, 1, 10)]) { SeriesIndex = 4 },
        ]);
        var cut = RenderPage();
        Assert.Empty(cut.FindAll(".series-progress .progress-book"));

        cut.Find(".series-progress .progress-toggle").Click();

        Assert.Equal(["#4 Mort", "#11 Reaper Man"], cut.FindAll(".series-progress .progress-book .progress-book-title").Select(e => e.TextContent));
        Assert.Equal(["read", "unread"], cut.FindAll(".series-progress .progress-book").Select(e => e.ClassList.Contains("is-read") ? "read" : "unread"));
        cut.Find(".series-progress .progress-toggle").Click();
        Assert.Empty(cut.FindAll(".series-progress .progress-book"));
    }
}
