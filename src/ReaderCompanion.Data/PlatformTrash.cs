using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

/// <summary>Picks the system trash for this machine: the Recycle Bin on Windows, the freedesktop trash on Linux and
/// BSD; elsewhere trashing is refused rather than guessed.</summary>
internal static class PlatformTrash
{
    public static ITrash ForThisMachine() =>
        OperatingSystem.IsWindows() ? new RecycleBinTrash()
        : OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD() ? FreedesktopTrash.ForCurrentUser()
        : new NoTrash();

    private sealed class NoTrash : ITrash
    {
        public bool MoveToTrash(string path) => false;
    }
}
