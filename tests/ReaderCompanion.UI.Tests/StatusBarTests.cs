using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.UI.Tests;

public class StatusBarTests : BunitContext
{
    private readonly FakeScanner _scanner = new();
    private readonly FakeDriveLibrary _drive = new();

    private void Fakes(List<BookListItem> books) => Services.AddAppFakes(books, scanner: _scanner, driveLibrary: _drive);

    [Fact]
    public void Count_ComesFromTheLibraryPage()
    {
        Fakes([
            new(1, "Dune", ["Frank Herbert"], null, null, false) { Genres = ["Science Fiction"] },
            new(2, "Mort", ["Terry Pratchett"], null, null, false) { Genres = ["Fantasy"] },
            new(3, "Sourcery", ["Terry Pratchett"], null, null, false) { Genres = ["Fantasy"] }]);

        var cut = Render<Routes>();

        cut.WaitForAssertion(() => Assert.Equal("3 books", cut.Find(".status-bar .status-count").TextContent));
        FilterSteps.Pick(cut, "filter-genre", "Fantasy");
        cut.WaitForAssertion(() => Assert.Equal("2 of 3 books", cut.Find(".status-bar .status-count").TextContent));
    }

    [Fact]
    public void Count_SaysNoBooks_ForAnEmptyLibrary()
    {
        Fakes([]);

        var cut = Render<Routes>();

        cut.WaitForAssertion(() => Assert.Equal("No books", cut.Find(".status-bar .status-count").TextContent));
    }

    [Fact]
    public void RunningScans_ShowOnEveryPage_BothWhenBothRun()
    {
        Fakes([]);
        _drive.ScanReports = new ScanProgress(3, 20);
        _drive.ScanHeld = new TaskCompletionSource<ScanSummary>();
        var status = Services.GetRequiredService<ScanStatus>();
        _ = status.Library.RunAsync(p => _scanner.ScanAsync(p));
        _scanner.Report(12, 40);
        _ = status.Drive.RunAsync(p => _drive.ScanAsync(p));
        Services.GetRequiredService<NavigationManager>().NavigateTo("settings");

        var cut = Render<Routes>();

        Assert.Equal(
            ["Scan 12 of 40 files", "Drive scan: Reading new books from Drive… 3 of 20"],
            cut.FindAll(".status-bar .status-scan").Select(s => s.TextContent));
        Assert.Empty(cut.FindAll(".status-bar .status-count")); // only the Library has a count
    }
}
