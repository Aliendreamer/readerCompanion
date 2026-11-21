using ReaderCompanion.Core.Scanning;
using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.UI.Library;

/// <summary>How Google Drive choices read on screen.</summary>
public static class DriveText
{
    public static string AccessLabel(DriveAccess access) => access switch
    {
        DriveAccess.PickedFiles => "Only books I pick",
        DriveAccess.WholeDriveReadOnly => "Whole Drive, read only",
        _ => "Whole Drive, read and write",
    };

    /// <summary>True for the failures a Drive action reports to the user instead of crashing.</summary>
    public static bool IsExpected(Exception e) => e is DriveUnavailableException or DriveSignInException or DriveLinkLostException
        or HttpRequestException or InvalidOperationException or IOException or UnauthorizedAccessException or OperationCanceledException;

    /// <summary>What to tell the user about an expected Drive failure.</summary>
    public static string Error(Exception e) =>
        e is OperationCanceledException ? "Google did not answer in time. Try again." : e.Message;

    /// <summary>What a Drive scan is doing, e.g. "Reading new books from Drive… 3 of 20".</summary>
    public static string ScanProgress(ScanProgress progress) => progress.Stage switch
    {
        ScanStage.Listing =>
            $"Looking through Drive… {BookText.Plural(progress.FoldersListed, "folder")}, {BookText.Plural(progress.FilesFound, "book")} so far",
        ScanStage.Checking => $"Checking books Drive has no checksum for… {progress.FilesProcessed} of {progress.FilesFound}",
        _ => $"Reading new books from Drive… {progress.FilesProcessed} of {progress.FilesFound}",
    };
}
