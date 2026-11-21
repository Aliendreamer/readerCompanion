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
}
