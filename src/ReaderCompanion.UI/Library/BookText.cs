using System.Globalization;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.UI.Library;

/// <summary>How a book's details read on screen, shared by every view.</summary>
public static class BookText
{
    public const string UnknownAuthor = "Unknown author";

    public static string Authors(BookListItem book) =>
        book.Authors.Count > 0 ? string.Join(", ", book.Authors) : UnknownAuthor;

    public static string? Series(BookListItem book) => book.Series is null ? null
        : book.SeriesIndex is { } index ? $"{book.Series} #{SeriesNumber(index)}"
        : book.Series;

    public static string StatusLabel(ReadStatus status) => status switch
    {
        ReadStatus.WantToRead => "Want to read",
        ReadStatus.Reading => "Reading",
        ReadStatus.Read => "Read",
        ReadStatus.Abandoned => "Abandoned",
        _ => "Not started",
    };

    /// <summary>"1 book", "3 books".</summary>
    public static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    /// <summary>A file size as shown on screen: 1.4 MB, 512 KB, 900 B.</summary>
    public static string FileSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => (bytes / (1024.0 * 1024)).ToString("0.0", CultureInfo.InvariantCulture) + " MB",
        >= 1024 => (bytes / 1024.0).ToString("0", CultureInfo.InvariantCulture) + " KB",
        _ => bytes + " B",
    };

    /// <summary>A file format as shown on screen: EPUB, PDF.</summary>
    public static string FormatLabel(BookFormat format) => format.ToString().ToUpperInvariant();

    /// <summary>When a book was added, as shown on screen: 4 Mar 2026.</summary>
    public static string DateAdded(BookListItem book) => Date(book.DateAddedUtc);

    /// <summary>A UTC time as the local day people read: 4 Mar 2026.</summary>
    public static string Date(DateTime utc) => utc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>A series position as people write it: 4, 2.5.</summary>
    public static string SeriesNumber(decimal index) => index.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The file's name, for a local path or a Drive file.</summary>
    public static string FileName(string path) =>
        ReaderCompanion.Core.Drive.DrivePath.IsDrive(path)
            ? ReaderCompanion.Core.Drive.DrivePath.Display(path).Split('/')[^1]
            : Path.GetFileName(path);

    /// <summary>Where the file is: its folder, or "Google Drive › folder" for a Drive file.</summary>
    public static string FileFolder(string path)
    {
        if (!ReaderCompanion.Core.Drive.DrivePath.IsDrive(path))
        {
            return Path.GetDirectoryName(path) ?? "";
        }

        var display = ReaderCompanion.Core.Drive.DrivePath.Display(path);
        var slash = display.LastIndexOf('/');
        return slash < 0 ? "Google Drive" : $"Google Drive › {display[..slash]}";
    }

    /// <summary>The whole path as the user knows it: a local path, or "Google Drive › Books/x.epub".</summary>
    public static string FileLocation(string path) =>
        ReaderCompanion.Core.Drive.DrivePath.IsDrive(path) ? $"Google Drive › {ReaderCompanion.Core.Drive.DrivePath.Display(path)}" : path;
}
