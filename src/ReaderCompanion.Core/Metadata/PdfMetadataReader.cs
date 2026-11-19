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

        return new BookMetadata
        {
            Title = NameNormalizer.Clean(info.Title),
            Authors = (info.Author ?? "")
                .Split(AuthorSeparators)
                .Select(NameNormalizer.Clean)
                .OfType<string>()
                .ToList(),
        };
    }
}
