using ReaderCompanion.Core.Metadata;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Writer;

namespace ReaderCompanion.Core.Tests;

public class PdfMetadataReaderTests
{
    [Fact]
    public void Properties_GiveTitleAndAuthor()
    {
        using var pdf = BuildPdf(title: "Dune", author: "Frank Herbert");

        var metadata = PdfMetadataReader.Read(pdf);

        Assert.Equal("Dune", metadata.Title);
        Assert.Equal(["Frank Herbert"], metadata.Authors);
    }

    [Fact]
    public void AuthorList_SplitsOnSemicolonAndAmpersand()
    {
        using var pdf = BuildPdf(title: "Anthology", author: "Ann Leckie; Becky Chambers & Martha Wells");

        Assert.Equal(["Ann Leckie", "Becky Chambers", "Martha Wells"], PdfMetadataReader.Read(pdf).Authors);
    }

    [Fact]
    public void EmptyProperties_GiveNoTitleAndNoAuthors()
    {
        using var pdf = BuildPdf(title: null, author: null);

        var metadata = PdfMetadataReader.Read(pdf);

        Assert.Null(metadata.Title);
        Assert.Empty(metadata.Authors);
    }

    internal static MemoryStream BuildPdf(string? title, string? author)
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        builder.DocumentInformation.Title = title;
        builder.DocumentInformation.Author = author;
        return new MemoryStream(builder.Build());
    }
}
