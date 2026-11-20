using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

/// <summary>Builds book files for tests that put books straight into the database.</summary>
internal static class TestFiles
{
    public static BookFile File(
        string path,
        FileStatus status = FileStatus.Present,
        BookFormat format = BookFormat.Epub,
        long sizeBytes = 1234) => new()
        {
            Path = path,
            Format = format,
            SizeBytes = sizeBytes,
            ContentHash = path,
            Status = status,
            LastWriteUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
        };
}
