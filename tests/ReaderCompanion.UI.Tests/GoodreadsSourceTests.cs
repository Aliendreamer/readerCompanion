using ReaderCompanion.UI.Library;

namespace ReaderCompanion.UI.Tests;

public class GoodreadsSourceTests : BunitContext
{
    private readonly FakeGoodreads _goodreads = new();
    private readonly FakePreferences _preferences = new();

    private IRenderedComponent<GoodreadsSource> RenderWith(params string?[] picked)
    {
        Services.AddAppFakes(preferences: _preferences, goodreads: _goodreads, filePicker: new FakeFilePicker(picked));
        return Render<GoodreadsSource>();
    }

    [Fact]
    public void ReviewList_YesAndNo_Answer()
    {
        _goodreads.Pending.Add(new(1, "Mort", ["Terry Pratchett"], "9780061020681", 5, "Mort", "9780552131063"));
        _goodreads.Pending.Add(new(2, "Dune", ["Frank Herbert"], "9780340960196", 6, "Dune", "9780441172719"));
        var cut = RenderWith();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".goodreads-review li").Count));
        Assert.Contains("9780552131063", cut.Find(".goodreads-review").TextContent);

        cut.FindAll(".review-same")[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".goodreads-review li")));
        cut.Find(".review-different").Click();

        Assert.Equal([(1, true), (2, false)], _goodreads.Answers);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".goodreads-review")));
    }

    [Theory]
    [InlineData("#review-all-same", true)]
    [InlineData("#review-all-different", false)]
    public void ReviewList_CanBeAnsweredAllAtOnce(string button, bool same)
    {
        _goodreads.Pending.Add(new(1, "Mort", ["Terry Pratchett"], null, 5, "Mort", null));
        _goodreads.Pending.Add(new(2, "Dune", ["Frank Herbert"], null, 6, "Dune", null));
        var cut = RenderWith();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".goodreads-review li").Count));

        cut.Find(button).Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".goodreads-review")));
        Assert.Equal([(1, same), (2, same)], _goodreads.Answers);
    }

    [Fact]
    public void Options_AreSaved_AndTheLastImportIsShown()
    {
        _preferences.Save(_preferences.Current with { GoodreadsLastImportUtc = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc) });
        var cut = RenderWith();

        cut.Find("#goodreads-shelves").Change(true);
        cut.Find("#goodreads-reminder").Change("6");

        Assert.True(_preferences.Current.GoodreadsShelvesAsCollections);
        Assert.Equal(6, _preferences.Current.GoodreadsReminderMonths);
        Assert.Contains("1 Aug 2026", cut.Find(".goodreads").TextContent);
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
