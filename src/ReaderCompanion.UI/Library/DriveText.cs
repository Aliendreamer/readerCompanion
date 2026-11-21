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
}
