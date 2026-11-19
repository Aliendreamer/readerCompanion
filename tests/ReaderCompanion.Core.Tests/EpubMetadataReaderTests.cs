using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Core.Tests;

public class EpubMetadataReaderTests
{
    private static readonly byte[] CoverJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    [Fact]
    public void FullyTaggedEpub3_ReadsEveryDetail()
    {
        using var epub = TestEpub.Build(
            """
            <dc:title>The Hobbit</dc:title>
            <dc:creator>J.R.R. Tolkien</dc:creator>
            <dc:creator>Christopher Tolkien</dc:creator>
            <dc:language>en</dc:language>
            <dc:identifier>urn:isbn:9780261103344</dc:identifier>
            <dc:subject>Fantasy</dc:subject>
            <dc:subject>Classics</dc:subject>
            <meta property="belongs-to-collection" id="c1">Middle-earth</meta>
            <meta refines="#c1" property="group-position">1</meta>
            """,
            """<item id="cover" href="images/cover.jpg" media-type="image/jpeg" properties="cover-image"/>""",
            ("OEBPS/images/cover.jpg", CoverJpeg));

        var metadata = EpubMetadataReader.Read(epub);

        Assert.Equal("The Hobbit", metadata.Title);
        Assert.Equal(["J.R.R. Tolkien", "Christopher Tolkien"], metadata.Authors);
        Assert.Equal("en", metadata.Language);
        Assert.Equal("9780261103344", metadata.Isbn);
        Assert.Equal(["Fantasy", "Classics"], metadata.Genres);
        Assert.Equal("Middle-earth", metadata.Series);
        Assert.Equal(1m, metadata.SeriesIndex);
        Assert.Equal(CoverJpeg, metadata.Cover!.Bytes);
        Assert.Equal(".jpg", metadata.Cover.Extension);
    }

    [Fact]
    public void CalibreSeriesMeta_ReadsSeriesAndIndex()
    {
        using var epub = TestEpub.Build(
            """
            <dc:title>Equal Rites</dc:title>
            <meta name="calibre:series" content="Discworld"/>
            <meta name="calibre:series_index" content="3.0"/>
            """);

        var metadata = EpubMetadataReader.Read(epub);

        Assert.Equal("Discworld", metadata.Series);
        Assert.Equal(3m, metadata.SeriesIndex);
    }

    [Fact]
    public void Epub2CoverMeta_ReadsCoverFromManifestId()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47];
        using var epub = TestEpub.Build(
            """<dc:title>Old</dc:title><meta name="cover" content="cover-img"/>""",
            """<item id="cover-img" href="Cover%20Art.png" media-type="image/png"/>""",
            ("OEBPS/Cover Art.png", png));

        var metadata = EpubMetadataReader.Read(epub);

        Assert.Equal(png, metadata.Cover!.Bytes);
        Assert.Equal(".png", metadata.Cover.Extension);
    }

    [Fact]
    public void CoverWithOddExtension_GetsExtensionFromMediaType()
    {
        using var epub = TestEpub.Build(
            "<dc:title>Odd</dc:title>",
            """<item id="c" href="cover.bin" media-type="image/png" properties="cover-image"/>""",
            ("OEBPS/cover.bin", [1, 2]));

        Assert.Equal(".png", EpubMetadataReader.Read(epub).Cover!.Extension);
    }

    [Fact]
    public void OversizedCover_IsSkipped()
    {
        using var epub = TestEpub.Build(
            "<dc:title>Huge</dc:title>",
            """<item id="c" href="c.jpg" media-type="image/jpeg" properties="cover-image"/>""",
            ("OEBPS/c.jpg", new byte[EpubMetadataReader.MaxCoverBytes + 1]));

        var metadata = EpubMetadataReader.Read(epub);

        Assert.Equal("Huge", metadata.Title);
        Assert.Null(metadata.Cover);
    }

    [Fact]
    public void CoverHrefWithParentSegment_IsResolvedAgainstOpfFolder()
    {
        byte[] jpg = [0xFF, 0xD8, 1];
        using var epub = TestEpub.Build(
            "<dc:title>Nested</dc:title>",
            """<item id="c" href="../Images/c.jpg" media-type="image/jpeg" properties="cover-image"/>""",
            ("Images/c.jpg", jpg));

        Assert.Equal(jpg, EpubMetadataReader.Read(epub).Cover!.Bytes);
    }

    [Theory]
    [InlineData("""<dc:identifier xmlns:opf="http://www.idpf.org/2007/opf" opf:scheme="ISBN">978-0-261-10334-4</dc:identifier>""", "9780261103344")]
    [InlineData("""<dc:identifier>0-261-10334-2</dc:identifier>""", "0261103342")]
    [InlineData("""<dc:identifier>urn:uuid:1b4e28ba-2fa1-11d2-883f-0016d3cca427</dc:identifier>""", null)]
    public void Isbn_IsRecognisedAndNormalised(string identifier, string? expected)
    {
        using var epub = TestEpub.Build("<dc:title>X</dc:title>" + identifier);

        Assert.Equal(expected, EpubMetadataReader.Read(epub).Isbn);
    }

    [Fact]
    public void WhitespaceOnlyValues_AreMissing()
    {
        using var epub = TestEpub.Build("<dc:title>   </dc:title><dc:creator> </dc:creator><dc:subject></dc:subject>");

        var metadata = EpubMetadataReader.Read(epub);

        Assert.Null(metadata.Title);
        Assert.Empty(metadata.Authors);
        Assert.Empty(metadata.Genres);
        Assert.Null(metadata.Cover);
    }
}
