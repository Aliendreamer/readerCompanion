using System.Runtime.Versioning;
using Microsoft.VisualBasic.FileIO;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data;

/// <summary>The Windows Recycle Bin.</summary>
[SupportedOSPlatform("windows")]
internal sealed class RecycleBinTrash : ITrash
{
    public bool MoveToTrash(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
            return !File.Exists(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
