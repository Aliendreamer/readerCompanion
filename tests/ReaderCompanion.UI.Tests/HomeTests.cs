using ReaderCompanion.UI.Pages;

namespace ReaderCompanion.UI.Tests;

public class HomeTests : BunitContext
{
    [Fact]
    public void Home_ShowsAppTitleAndEmptyLibraryMessage()
    {
        var cut = Render<Home>();

        Assert.Equal("readerCompanion", cut.Find("h1").TextContent);
        Assert.Contains("Your library is empty.", cut.Markup);
    }

    [Fact]
    public void Routes_AtRoot_RendersHomeInsideLayout()
    {
        var cut = Render<Routes>();

        Assert.Equal("readerCompanion", cut.Find("main h1").TextContent);
    }
}
