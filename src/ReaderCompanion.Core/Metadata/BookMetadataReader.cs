using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Result of reading a file: its details (never without a title) and whether reading failed.</summary>
public sealed record MetadataResult(BookMetadata Metadata, bool Unreadable, string? Error = null);

/// <summary>Picks the reader by file extension, applies the file-name title fallback, and never throws.</summary>
public static class BookMetadataReader
{
    public static bool IsSupported(string fileName) => TryFormatOf(fileName) is not null;

    public static BookFormat FormatOf(string fileName) =>
        TryFormatOf(fileName) ?? throw new ArgumentException($"Not an EPUB or PDF file: {fileName}", nameof(fileName));

    public static MetadataResult Read(Stream content, string fileName)
    {
        var format = FormatOf(fileName);
        var fallbackTitle = Path.GetFileNameWithoutExtension(fileName);
        try
        {
            var metadata = format == BookFormat.Epub ? EpubMetadataReader.Read(content) : PdfMetadataReader.Read(content);
            return new MetadataResult(metadata with { Title = metadata.Title ?? fallbackTitle }, Unreadable: false);
        }
        catch (Exception ex)
        {
            return new MetadataResult(new BookMetadata { Title = fallbackTitle }, Unreadable: true, ex.Message);
        }
    }

    private static BookFormat? TryFormatOf(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".epub" => BookFormat.Epub,
            ".pdf" => BookFormat.Pdf,
            _ => null,
        };
}
