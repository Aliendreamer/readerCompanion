using Microsoft.EntityFrameworkCore;
using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Data;

internal static class KnownFiles
{
    /// <summary>The library's files, local and on Drive. Each scan covers one side; the other side only lets new
    /// content join a book it already has.</summary>
    public static (List<KnownFile> Local, List<KnownFile> Drive) Load(LibraryDbContext db)
    {
        var all = db.BookFiles.AsNoTracking()
            .Select(f => new { File = new KnownFile(f.Id, f.BookId, f.Path, f.SizeBytes, f.LastWriteUtc, f.ContentHash, f.Status), OnDrive = f.DriveFileId != null })
            .ToLookup(f => f.OnDrive, f => f.File);
        return ([.. all[false]], [.. all[true]]);
    }
}
