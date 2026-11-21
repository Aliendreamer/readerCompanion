using UglyToad.PdfPig;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Reads title and authors from a PDF's document properties.</summary>
public static class PdfMetadataReader
{
    private static readonly char[] AuthorSeparators = [';', '&'];

    public static BookMetadata Read(Stream pdf)
    {
        using var document = PdfDocument.Open(pdf);
        var info = document.Information;

        return new BookMetadata { Title = NameNormalizer.Clean(info.Title), Authors = Authors(info.Author) };
    }

    /// <summary>The Author property's names: several are separated by ";" or "&amp;".</summary>
    internal static List<string> Authors(string? author) =>
        [.. (author ?? "").Split(AuthorSeparators).Select(NameNormalizer.Clean).OfType<string>()];
}
