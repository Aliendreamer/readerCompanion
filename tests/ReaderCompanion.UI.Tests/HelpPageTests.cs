using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class HelpPageTests : BunitContext
{
    public HelpPageTests() => Services.AddAppFakes();

    [Fact]
    public void Explains_LinkingDrive_AndGettingTheGoodreadsExport()
    {
        var cut = Render<HelpPage>();

        Assert.Equal(
            ["Add your books", "Link Google Drive", "Import from Goodreads", "Make text larger"],
            cut.FindAll(".help-topic h2").Select(h => h.TextContent));
        var drive = cut.Find("#help-drive").TextContent;
        Assert.Contains("Link Google Drive", drive);
        Assert.Contains("hasn’t verified this app", drive);
        var goodreads = cut.Find("#help-goodreads").TextContent;
        Assert.Contains("Export library", goodreads);
        Assert.Contains(".csv", goodreads);
        Assert.Contains("Import export file…", goodreads);
    }

    [Fact]
    public void Topics_AreListedAtTheTop_AsLinks()
    {
        var cut = Render<HelpPage>();

        Assert.Equal(["#help-books", "#help-drive", "#help-goodreads", "#help-text-size"],
            cut.FindAll(".help-contents a").Select(a => a.GetAttribute("href")));
    }
}
