using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Core.Tests;

public class ScanReconcilerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static KnownFile Known(int id, int book, string path, string hash, FileStatus status = FileStatus.Present) =>
        new(id, book, path, 10, T0, hash, status);

    private static FoundFile Found(string path, string hash) => new(path, 10, T0, hash);

    private static ScanPlan Plan(IEnumerable<KnownFile> known, IEnumerable<FoundFile> found, params string[] unavailable) =>
        ScanReconciler.Plan([.. known], [.. found], unavailable);

    [Fact]
    public void ContentOfABookElsewhere_JoinsIt_AndThoseFilesAreNeverMarkedMissing()
    {
        var onDrive = Known(1, 7, "gdrive://d1/Books/a.epub", "h1");

        var plan = ScanReconciler.Plan([], [Found("/b/a.epub", "h1")], [], elsewhere: [onDrive]);

        Assert.Equal([new JoinBook(7, Found("/b/a.epub", "h1"))], plan.Actions);
    }

    [Fact]
    public void SamePathSameContent_IsKept()
    {
        var file = Known(1, 1, "/b/a.epub", "h1");

        var plan = Plan([file], [Found("/b/a.epub", "h1")]);

        Assert.Equal([new KeepFile(file)], plan.Actions);
    }

    [Fact]
    public void SamePathSameContent_ThatCouldNotBeRead_IsReadAgain()
    {
        var file = Known(1, 1, "/b/a.epub", "h1") with { Unreadable = true };

        var plan = Plan([file], [Found("/b/a.epub", "h1")]);

        Assert.Equal([new UpdateContent(file, Found("/b/a.epub", "h1"))], plan.Actions);
    }

    [Fact]
    public void SameContentNewPath_IsMoved()
    {
        var file = Known(1, 1, "/b/old.epub", "h1");
        var found = Found("/b/new.epub", "h1");

        var plan = Plan([file], [found]);

        Assert.Equal([new MoveFile(file, found)], plan.Actions);
    }

    [Fact]
    public void SamePathNewContent_UpdatesBookInPlace()
    {
        var file = Known(1, 1, "/b/a.epub", "h1");
        var found = Found("/b/a.epub", "h2");

        var plan = Plan([file], [found]);

        Assert.Equal([new UpdateContent(file, found)], plan.Actions);
    }

    [Fact]
    public void ChangedWhileOtherCopyStillPresent_BecomesNewBook()
    {
        var edited = Known(1, 1, "/b/a.epub", "h1");
        var copy = Known(2, 1, "/b/backup/a.epub", "h1");
        var found = Found("/b/a.epub", "h2");

        var plan = Plan([edited, copy], [found, Found("/b/backup/a.epub", "h1")]);

        Assert.Contains(new SplitToNewBook(edited, found), plan.Actions);
        Assert.Contains(new KeepFile(copy), plan.Actions);
    }

    [Fact]
    public void MergedBook_OneFileChanged_UpdatesInPlace()
    {
        var epub = Known(1, 1, "/b/mort.epub", "h-epub");
        var pdf = Known(2, 1, "/b/mort.pdf", "h-pdf");
        var newEdition = Found("/b/mort.epub", "h-epub-2");

        var plan = Plan([epub, pdf], [newEdition, Found("/b/mort.pdf", "h-pdf")]);

        Assert.Contains(new UpdateContent(epub, newEdition), plan.Actions);
        Assert.DoesNotContain(plan.Actions, a => a is SplitToNewBook);
    }

    [Fact]
    public void MergedBook_DifferentContentJoinedThisScan_StillUpdatesInPlace()
    {
        var epub = Known(1, 1, "/b/mort.epub", "h-epub");
        var pdf = Known(2, 1, "/b/mort.pdf", "h-pdf");
        var pdfCopy = Found("/b/backup/mort.pdf", "h-pdf");
        var newEdition = Found("/b/mort.epub", "h-epub-2");

        var plan = Plan([epub, pdf], [newEdition, Found("/b/mort.pdf", "h-pdf"), pdfCopy]);

        Assert.Contains(new JoinBook(1, pdfCopy), plan.Actions);
        Assert.Contains(new UpdateContent(epub, newEdition), plan.Actions);
    }

    [Fact]
    public void OldVersionCopiedAway_OriginalReplaced_NewVersionBecomesNewBook()
    {
        var original = Known(1, 1, "/b/a.epub", "h1");
        var keptCopy = Found("/b/a-old.epub", "h1");
        var replaced = Found("/b/a.epub", "h2");

        var plan = Plan([original], [keptCopy, replaced]);

        Assert.Contains(new JoinBook(1, keptCopy), plan.Actions);
        Assert.Contains(new SplitToNewBook(original, replaced), plan.Actions);
    }

    [Fact]
    public void ChangedToContentOfAnotherBook_JoinsThatBook()
    {
        var a = Known(1, 1, "/b/a.epub", "h1");
        var b = Known(2, 2, "/b/b.epub", "h2");
        var found = Found("/b/a.epub", "h2");

        var plan = Plan([a, b], [found, Found("/b/b.epub", "h2")]);

        Assert.Contains(new ReassignFile(a, 2, found), plan.Actions);
    }

    [Fact]
    public void UnknownContent_AddsOneBookPerContent()
    {
        var one = Found("/b/one.epub", "h1");
        var copyOfOne = Found("/b/x/one.epub", "h1");
        var two = Found("/b/two.pdf", "h2");

        var plan = Plan([], [one, copyOfOne, two]);

        var added = plan.Actions.OfType<AddBook>().ToList();
        Assert.Equal(2, added.Count);
        Assert.Contains(added, a => a.Files.Select(f => f.Path).Order().SequenceEqual(["/b/one.epub", "/b/x/one.epub"]));
        Assert.Contains(added, a => a.Files.Count == 1 && a.Files[0] == two);
    }

    [Fact]
    public void NewPathWithKnownContent_StillAtOldPath_JoinsAsCopy()
    {
        var file = Known(1, 7, "/b/a.epub", "h1");
        var copy = Found("/b/backup/a.epub", "h1");

        var plan = Plan([file], [Found("/b/a.epub", "h1"), copy]);

        Assert.Contains(new JoinBook(7, copy), plan.Actions);
        Assert.Contains(new KeepFile(file), plan.Actions);
    }

    [Fact]
    public void KnownFileNotFound_IsMarkedMissing()
    {
        var file = Known(1, 1, "/b/a.epub", "h1");

        var plan = Plan([file], []);

        Assert.Equal([new MarkMissing(file)], plan.Actions);
    }

    [Fact]
    public void AlreadyMissingAndStillGone_NeedsNoAction()
    {
        var plan = Plan([Known(1, 1, "/b/a.epub", "h1", FileStatus.Missing)], []);

        Assert.Empty(plan.Actions);
    }

    [Fact]
    public void MissingFileBackAtSamePath_Reappears()
    {
        var file = Known(1, 1, "/b/a.epub", "h1", FileStatus.Missing);

        var plan = Plan([file], [Found("/b/a.epub", "h1")]);

        Assert.Equal([new Reappear(file)], plan.Actions);
    }

    [Fact]
    public void MissingFileBackAtOtherPath_IsMoved()
    {
        var file = Known(1, 1, "/b/a.epub", "h1", FileStatus.Missing);
        var found = Found("/b/elsewhere/a.epub", "h1");

        var plan = Plan([file], [found]);

        Assert.Equal([new MoveFile(file, found)], plan.Actions);
    }

    [Fact]
    public void FilesUnderUnavailableRoot_AreLeftUntouched()
    {
        var offline = Known(1, 1, "/usb/books/a.epub", "h1");
        var local = Known(2, 2, "/home/b.epub", "h2");

        var plan = Plan([offline, local], [], "/usb/books");

        Assert.Equal([new MarkMissing(local)], plan.Actions);
    }

    [Fact]
    public void IsUnchanged_ComparesSizeAndWriteTime()
    {
        var file = Known(1, 1, "/b/a.epub", "h1");

        Assert.True(ScanReconciler.IsUnchanged(file, 10, T0));
        Assert.False(ScanReconciler.IsUnchanged(file, 11, T0));
        Assert.False(ScanReconciler.IsUnchanged(file, 10, T0.AddSeconds(1)));
        Assert.False(ScanReconciler.IsUnchanged(null, 10, T0));
    }
}
