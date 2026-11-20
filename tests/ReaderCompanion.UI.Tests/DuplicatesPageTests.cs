using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class DuplicatesPageTests : BunitContext
{
    private readonly FakeDuplicates _duplicates = new();

    public DuplicatesPageTests() => Services.AddAppFakes(duplicates: _duplicates);

    private static BookDetails Details(
        int id, string title, string[] authors, BookFormat format, bool edited = false, string[]? genres = null,
        string? isbn = null) =>
        new(new BookListItem(id, title, authors, null, null, false) { Genres = genres ?? [], Formats = [format] },
            null, isbn, edited, [new BookFileInfo($"/b/{id}.{format.ToString().ToLowerInvariant()}", format, 10, true)]);

    private IRenderedComponent<Duplicates> RenderPage()
    {
        var cut = Render<Duplicates>();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".loading")));
        return cut;
    }

    [Fact]
    public void NoGroups_SaysSo()
    {
        var cut = RenderPage();

        Assert.Contains("No likely duplicates", cut.Markup);
    }

    [Fact]
    public void Group_ShowsBooksSideBySide_WithFilesAndFormats()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["Terry Pratchett"], BookFormat.Epub),
            Details(2, "MORT", ["terry pratchett"], BookFormat.Pdf)]));

        var cut = RenderPage();

        var books = cut.FindAll(".dup-group .dup-book");
        Assert.Equal(2, books.Count);
        Assert.Contains("Mort", books[0].TextContent);
        Assert.Contains("EPUB", books[0].TextContent);
        Assert.Contains("/b/2.pdf", books[1].TextContent);
    }

    [Fact]
    public void Merge_AsksFirst_ThenMergesIntoThePickedBook()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["Terry Pratchett"], BookFormat.Epub),
            Details(2, "MORT", [], BookFormat.Pdf)]));
        var cut = RenderPage();

        cut.Find(".dup-merge").Click();
        Assert.Contains("Merge into “Mort”?", cut.Find(".dup-group").TextContent);
        Assert.Empty(_duplicates.Merges);

        cut.Find(".dup-confirm").Click();

        var (keep, others) = Assert.Single(_duplicates.Merges);
        Assert.Equal(1, keep);
        Assert.Equal([2], others);
        cut.WaitForAssertion(() => Assert.Contains("No likely duplicates", cut.Markup));
    }

    [Fact]
    public void Merge_Failure_IsShown_AndTheGroupStays()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["A"], BookFormat.Epub), Details(2, "Mort", ["A"], BookFormat.Pdf)]));
        _duplicates.Fail = true;
        var cut = RenderPage();

        cut.Find(".dup-merge").Click();
        cut.Find(".dup-confirm").Click();

        Assert.Contains("could not", cut.Find(".panel-error").TextContent);
        Assert.Single(cut.FindAll(".dup-group"));
    }

    [Fact]
    public void Merge_Cancel_DoesNothing()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["A"], BookFormat.Epub), Details(2, "Mort", ["A"], BookFormat.Pdf)]));
        var cut = RenderPage();

        cut.Find(".dup-merge").Click();
        cut.Find(".dup-cancel").Click();

        Assert.Empty(_duplicates.Merges);
        Assert.NotNull(cut.Find(".dup-merge"));
    }

    [Fact]
    public void PickingAnotherBook_MergesIntoThatOne()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["A"], BookFormat.Epub), Details(2, "Mort", ["A"], BookFormat.Pdf)]));
        var cut = RenderPage();

        cut.FindAll(".dup-book input[type=radio]")[1].Change(true);
        cut.Find(".dup-merge").Click();
        cut.Find(".dup-confirm").Click();

        Assert.Equal(2, Assert.Single(_duplicates.Merges).Keep);
    }

    [Fact]
    public void NotDuplicates_DismissesTheGroup()
    {
        _duplicates.Groups.Add(new([Details(1, "Mort", ["A"], BookFormat.Epub), Details(2, "Mort", ["A"], BookFormat.Pdf)]));
        var cut = RenderPage();

        cut.Find(".dup-dismiss").Click();

        Assert.Equal([1, 2], Assert.Single(_duplicates.Dismissals).Order());
        cut.WaitForAssertion(() => Assert.Contains("No likely duplicates", cut.Markup));
    }

    [Fact]
    public void DefaultPick_PrefersTheEditedBook_ThenTheMostDetailed_ThenEpub()
    {
        Assert.Equal(2, DuplicatePick.Default([Details(1, "A", ["X"], BookFormat.Epub, genres: ["G"]),
            Details(2, "A", [], BookFormat.Pdf, edited: true)]));
        Assert.Equal(2, DuplicatePick.Default([Details(1, "A", [], BookFormat.Epub),
            Details(2, "A", ["X"], BookFormat.Pdf, isbn: "1")]));
        Assert.Equal(2, DuplicatePick.Default([Details(1, "A", ["X"], BookFormat.Pdf),
            Details(2, "A", ["X"], BookFormat.Epub)]));
        Assert.Equal(1, DuplicatePick.Default([Details(1, "A", ["X"], BookFormat.Epub),
            Details(2, "A", ["X"], BookFormat.Epub)]));
    }
}
