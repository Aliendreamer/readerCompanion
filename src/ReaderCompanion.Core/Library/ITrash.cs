namespace ReaderCompanion.Core.Library;

public enum TrashResult
{
    Trashed,

    /// <summary>Refused: it is the book's last file.</summary>
    LastFile,

    NotFound,

    /// <summary>The system trash could not take it; nothing changed.</summary>
    TrashFailed,

    /// <summary>Refused: the file is on Google Drive and the app's access there is read only.</summary>
    ReadOnly,
}

/// <summary>The operating system's trash, from where the user can restore what the app put there.</summary>
public interface ITrash
{
    /// <summary>Moves the file to the trash; false when it is not there or the trash cannot take it.</summary>
    bool MoveToTrash(string path);
}
