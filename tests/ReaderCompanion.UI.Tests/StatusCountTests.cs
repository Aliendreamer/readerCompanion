using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>Each filter value says how many books have it; want to read also how many have files.</summary>
public class StatusCountTests : BunitContext
{
    public StatusCountTests() => Services.AddAppFakes(
    [
        new(1, "Dune", ["Frank Herbert"], null, null, false) { ReadStatus = ReadStatus.Read },
        new(2, "Mort", ["Terry Pratchett"], null, null, false) { ReadStatus = ReadStatus.WantToRead },
        new(3, "Wanted", ["Some Writer"], null, null, false) { ReadStatus = ReadStatus.WantToRead, Owned = false },
        new(4, "Also wanted", ["Some Writer"], null, null, false) { ReadStatus = ReadStatus.WantToRead, Owned = false },
    ]);

    [Fact]
    public void WantToRead_ShowsItsCount_AndHowManyHaveFiles()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));

        FilterSteps.Open(cut, "filter-status");
        var rows = cut.FindAll("#filter-status-menu li").ToDictionary(
            li => li.QuerySelector(".filter-option-name")!.TextContent.Trim(),
            li => (li.QuerySelector(".filter-option-count")?.TextContent.Trim(), li.QuerySelector(".filter-note")?.TextContent.Trim()));

        Assert.Equal(("3", "1 with files, 2 without"), rows["Want to read"]);
        Assert.Equal(("1", null), rows["Read"]);
        Assert.Equal((null, null), rows["Reading"]); // none: no count
    }

    [Fact]
    public void EveryValue_ShowsHowManyBooksHaveIt()
    {
        var cut = Render<Home>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".book-title")));

        FilterSteps.Open(cut, "filter-author");
        var counts = cut.FindAll("#filter-author-menu li").ToDictionary(
            li => li.QuerySelector(".filter-option-name")!.TextContent.Trim(), li => li.QuerySelector(".filter-option-count")!.TextContent.Trim());

        Assert.Equal("2", counts["Some Writer"]);
        Assert.Equal("1", counts["Frank Herbert"]);
    }
}
