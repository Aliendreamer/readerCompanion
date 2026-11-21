using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Organising;
using ReaderCompanion.Core.Scanning;
using ReaderCompanion.UI.Library;
using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

/// <summary>Every long list shows a page of <see cref="Pager.PageSize"/> items, and more near its end.</summary>
public class PagingTests : BunitContext
{
    private readonly FakeInView _inView = new();
    private readonly FakeOrganiser _organiser = new();
    private readonly FakeDuplicates _duplicates = new();
    private readonly FakeFolders _folders = new();

    public PagingTests() => Services.AddAppFakes(organiser: _organiser, duplicates: _duplicates, folders: _folders, inView: _inView);

    [Fact]
    public void Pager_GivesAPage_MoreOnRequest_AndStartsOverForANewList()
    {
        var pager = new Pager();
        int[] first = [.. Enumerable.Range(1, 300)];

        Assert.Equal(120, pager.Page(first).Count);
        pager.More();
        Assert.Equal(240, pager.Page(first).Count);
        Assert.Equal(120, pager.Page((int[])[.. Enumerable.Range(1, 300)]).Count); // a new list
        Assert.Equal(5, pager.Page((int[])[1, 2, 3, 4, 5]).Count);
    }

    [Fact]
    public void ScanSummary_ListsUnreadableFilesAPageAtATime()
    {
        var summary = new ScanSummary { Unreadable = [.. Enumerable.Range(1, 250).Select(i => new UnreadableFile($"/b/{i}.epub", "bad zip"))] };

        var cut = Render<ScanSummaryView>(p => p.Add(c => c.Summary, summary));

        Assert.Equal(120, cut.FindAll(".summary li").Count);
        cut.Find(".show-more").Click();
        Assert.Equal(240, cut.FindAll(".summary li").Count);
        cut.InvokeAsync(_inView.ScrollNear);
        cut.WaitForAssertion(() => Assert.Equal(250, cut.FindAll(".summary li").Count));
        Assert.Empty(cut.FindAll(".load-more"));
    }

    [Fact]
    public void OrganisePreview_ListsMovesAPageAtATime()
    {
        _folders.Linked.Add("/books");
        _organiser.Planned.AddRange(Enumerable.Range(1, 130).Select(i => new Move(i, $"/books/{i}.epub", $"/books/A/{i}.epub")));
        var cut = Render<OrganisePage>();
        cut.WaitForAssertion(() => cut.Find("#organise-preview"));

        cut.Find("#organise-preview").Click();

        cut.WaitForAssertion(() => Assert.Equal(120, cut.FindAll(".organise-moves tbody tr").Count));
        Assert.Contains("Showing 120 of 130", cut.Find(".load-more").TextContent);
        Assert.Contains("Move 130 files", cut.Find("#organise-apply").TextContent); // all are still selected
    }

    [Fact]
    public void Duplicates_ListsGroupsAPageAtATime()
    {
        for (var i = 0; i < 125; i++)
        {
            _duplicates.Groups.Add(new([Book(i * 2, "Mort"), Book((i * 2) + 1, "MORT")]));
        }

        var cut = Render<Duplicates>();

        cut.WaitForAssertion(() => Assert.Equal(120, cut.FindAll(".dup-group").Count));
        cut.Find(".show-more").Click();
        Assert.Equal(125, cut.FindAll(".dup-group").Count);
    }

    [Fact]
    public void FilterOptions_ListAPageAtATime_AndASearchStartsOver()
    {
        string[] authors = [.. Enumerable.Range(1, 300).Select(i => $"Author {i:000}")];
        var cut = Render<FilterPicker<string>>(p => p.Add(c => c.Id, "filter-author").Add(c => c.Label, "Author").Add(c => c.Options, authors));

        FilterSteps.Open(cut, "filter-author");
        Assert.Equal(120, cut.FindAll("#filter-author-menu .filter-option").Count);
        cut.Find("#filter-author-menu .show-more").Click();
        Assert.Equal(240, cut.FindAll("#filter-author-menu .filter-option").Count);

        cut.Find("#filter-author-menu .filter-search").Input("Author 1");

        Assert.Equal(100, cut.FindAll("#filter-author-menu .filter-option").Count); // 100-199: all of them, one page
    }

    private static BookDetails Book(int id, string title) =>
        new(new BookListItem(id, title, ["Terry Pratchett"], null, null, false) { Formats = [BookFormat.Epub] },
            null, null, false, [new BookFileInfo($"/b/{id}.epub", BookFormat.Epub, 10, true)]);
}
