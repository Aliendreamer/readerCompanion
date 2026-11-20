using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class GoodreadsSourceTests : BunitContext
{
    private readonly FakeGoodreads _goodreads = new();

    private IRenderedComponent<GoodreadsSource> RenderWith(params string?[] picked)
    {
        Services.AddAppFakes(goodreads: _goodreads, filePicker: new FakeFilePicker(picked));
        return Render<GoodreadsSource>();
    }

    [Fact]
    public void Import_ShowsTheSummary()
    {
        var cut = RenderWith("/home/me/goodreads_library_export.csv");

        cut.Find("#goodreads-import").Click();

        cut.WaitForAssertion(() => Assert.Contains("12 matched", cut.Find(".goodreads-summary").TextContent));
        Assert.Equal(["/home/me/goodreads_library_export.csv"], _goodreads.Imported);
        Assert.Contains("30 added as wanted", cut.Find(".goodreads-summary").TextContent);
        Assert.Contains("4 updated", cut.Find(".goodreads-summary").TextContent);
        Assert.Contains("appears more than once", cut.Find(".goodreads-summary").TextContent);
    }

    [Fact]
    public void CancelledPicker_DoesNothing()
    {
        var cut = RenderWith((string?)null);

        cut.Find("#goodreads-import").Click();

        Assert.Empty(_goodreads.Imported);
        Assert.Empty(cut.FindAll(".goodreads-summary"));
    }

    [Fact]
    public void NotAnExport_IsShown()
    {
        _goodreads.Failure = new InvalidDataException("This is not a Goodreads library export (its columns are missing).");
        var cut = RenderWith("/home/me/photo.csv");

        cut.Find("#goodreads-import").Click();

        cut.WaitForAssertion(() => Assert.Contains("not a Goodreads library export", cut.Find(".panel-error").TextContent));
    }
}
