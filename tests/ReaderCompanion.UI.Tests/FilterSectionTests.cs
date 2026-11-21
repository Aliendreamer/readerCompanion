using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class FilterSectionTests : BunitContext
{
    public FilterSectionTests()
    {
        Services.AddSingleton<IInView>(new FakeInView());
        Services.AddSingleton<FilterTreeState>();
    }

    private static readonly string[] Authors = [.. Enumerable.Range(1, 12).Select(i => $"Author {i:00}")];

    [Fact]
    public void LongList_HasASearch_ThatNarrowsTheOptions()
    {
        var cut = Render<FilterSection<string>>(p => p
            .Add(c => c.Id, "filter-author").Add(c => c.Label, "Author").Add(c => c.Options, Authors));

        FilterSteps.Open(cut, "filter-author");
        cut.Find("#filter-author-menu .filter-search").Input("1");

        Assert.Equal(["Author 01", "Author 10", "Author 11", "Author 12"], FilterSteps.Options(cut, "filter-author"));
    }

    [Fact]
    public void ShortList_HasNoSearch()
    {
        var cut = Render<FilterSection<string>>(p => p
            .Add(c => c.Id, "filter-genre").Add(c => c.Label, "Genre").Add(c => c.Options, ["Fantasy", "Horror"]));

        FilterSteps.Open(cut, "filter-genre");

        Assert.Empty(cut.FindAll(".filter-search"));
        Assert.Equal("true", cut.Find("#filter-genre").GetAttribute("aria-expanded"));
    }
}
