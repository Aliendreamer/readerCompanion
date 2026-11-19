using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Core.Tests;

public class BookMetadataReaderTests
{
    [Fact]
    public void PdfWithoutProperties_TitleFromFileName_NoAuthor()
    {
        using var pdf = PdfMetadataReaderTests.BuildPdf(title: null, author: null);

        var result = BookMetadataReader.Read(pdf, "Neuromancer.pdf");

        Assert.False(result.Unreadable);
        Assert.Equal("Neuromancer", result.Metadata.Title);
        Assert.Empty(result.Metadata.Authors);
    }

    [Fact]
    public void CorruptEpub_IsUnreadable_TitledFromFileName()
    {
        using var junk = new MemoryStream([1, 2, 3, 4, 5]);

        var result = BookMetadataReader.Read(junk, "broken.epub");

        Assert.True(result.Unreadable);
        Assert.Equal("broken", result.Metadata.Title);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void TaggedEpub_KeepsItsOwnTitle()
    {
        using var epub = TestEpub.Build("<dc:title>Real Title</dc:title>");

        Assert.Equal("Real Title", BookMetadataReader.Read(epub, "file-name.EPUB").Metadata.Title);
    }

    [Theory]
    [InlineData("a.epub", true)]
    [InlineData("B.EPUB", true)]
    [InlineData("c.Pdf", true)]
    [InlineData("notes.txt", false)]
    [InlineData("epub", false)]
    public void IsSupported_ByExtension_CaseInsensitive(string fileName, bool expected) =>
        Assert.Equal(expected, BookMetadataReader.IsSupported(fileName));

    [Theory]
    [InlineData("a.EPUB", BookFormat.Epub)]
    [InlineData("a.pdf", BookFormat.Pdf)]
    public void FormatOf_ByExtension(string fileName, BookFormat expected) =>
        Assert.Equal(expected, BookMetadataReader.FormatOf(fileName));
}
