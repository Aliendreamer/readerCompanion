namespace ReaderCompanion.UI;

/// <summary>Hands files to the operating system. Each call returns false when the system could not do it.</summary>
public interface IFileLauncher
{
    /// <summary>Opens the file in the system's default app for its type.</summary>
    bool Open(string path);

    /// <summary>Opens the folder that holds the file.</summary>
    bool ShowInFolder(string path);
}
