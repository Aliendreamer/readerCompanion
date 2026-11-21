using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>The Status section says how many books have each status, and how many want-to-read books have files.</summary>
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
        var notes = cut.FindAll("#filter-status-menu li").ToDictionary(
            li => li.QuerySelector(".filter-option")!.TextContent.Trim(), li => li.QuerySelector(".filter-note")?.TextContent.Trim());

        Assert.Equal("3 · 1 with files, 2 without", notes["Want to read"]);
        Assert.Equal("1", notes["Read"]);
        Assert.Null(notes["Reading"]); // none: no count
    }
}
