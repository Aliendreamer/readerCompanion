using ReaderCompanion.Core.Library;
using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class BookCoverTests : BunitContext
{
    public BookCoverTests() => Services.AddAppFakes();

    [Fact]
    public void WithCover_ShowsImageFromCoverUrl()
    {
        var cut = Render<BookCover>(p => p.Add(c => c.Book, Book("Mort", cover: "abc.jpg")));

        var img = cut.Find("img.cover-image");
        Assert.Equal("test-cover://abc.jpg", img.GetAttribute("src"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Empty(cut.FindAll(".cover-placeholder"));
    }

    [Fact]
    public void WithoutCover_ShowsPlaceholderWithTitleAndFirstAuthor()
    {
        var cut = Render<BookCover>(p => p.Add(c => c.Book, Book("Neuromancer", authors: ["William Gibson", "X"])));

        Assert.Equal("Neuromancer", cut.Find(".cover-placeholder .placeholder-title").TextContent);
        Assert.Equal("William Gibson", cut.Find(".cover-placeholder .placeholder-author").TextContent);
        Assert.Empty(cut.FindAll("img"));
    }

    [Fact]
    public void CoverThatFailsToLoad_FallsBackToPlaceholder()
    {
        var cut = Render<BookCover>(p => p.Add(c => c.Book, Book("Mort", cover: "gone.jpg")));

        cut.Find("img.cover-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        Assert.Equal("Mort", cut.Find(".cover-placeholder .placeholder-title").TextContent);
    }

    [Fact]
    public void PlaceholderColour_IsStablePerTitle()
    {
        Assert.Equal(CoverColors.HueFor("Neuromancer"), CoverColors.HueFor("Neuromancer"));
        Assert.Equal(CoverColors.HueFor("Neuromancer"), CoverColors.HueFor("  neuromancer "));
        Assert.NotEqual(CoverColors.HueFor("Neuromancer"), CoverColors.HueFor("Dune"));
        Assert.InRange(CoverColors.HueFor("Dune"), 0, 359);
    }

    [Fact]
    public void Placeholder_UsesTitleHue()
    {
        var cut = Render<BookCover>(p => p.Add(c => c.Book, Book("Dune")));

        Assert.Contains($"--cover-hue: {CoverColors.HueFor("Dune")}", cut.Find(".cover-placeholder").GetAttribute("style"));
    }

    private static BookListItem Book(string title, string? cover = null, string[]? authors = null) =>
        new(1, title, authors ?? [], null, null, false, cover);
}
