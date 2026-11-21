using System.Text;
using ReaderCompanion.Core.Metadata;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace ReaderCompanion.Core.Tests;

public class PdfInfoReaderTests
{
    private const string Catalog = "<< /Type /Catalog /Pages 2 0 R >>";
    private const string Pages = "<< /Type /Pages /Kids [] /Count 0 >>";

    private static string Utf16Hex(string text) => "<FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(text)) + ">";

    [Fact]
    public void APdfFromAWriter_IsReadFromALittleOfTheFile()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        for (var i = 0; i < 300; i++)
        {
            builder.AddPage(PageSize.A4).AddText($"Page {i} with some words on it to give it weight.", 10, new PdfPoint(40, 700), font);
        }

        builder.DocumentInformation.Title = "Neuromancer";
        builder.DocumentInformation.Author = "William Gibson; Someone Else";
        var bytes = builder.Build();
        using var pdf = new CountingStream(bytes);

        var metadata = PdfInfoReader.TryRead(pdf);

        Assert.Equal("Neuromancer", metadata!.Title);
        Assert.Equal(["William Gibson", "Someone Else"], metadata.Authors);
        Assert.True(pdf.BytesRead < bytes.Length / 4, $"{pdf.BytesRead} of {bytes.Length} bytes read");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ACrossReferenceStream_WithTheInformationPackedInAnObjectStream(bool predictor)
    {
        var pdf = RawPdf.WithXrefStream(
            new() { [1] = Catalog, [2] = Pages },
            new() { [3] = $"<< /Title (Programming = \\(++\\)Algorithms;) /Author {Utf16Hex("Преслав Наков & Панайот Добриков")} >>" },
            "/Root 1 0 R /Info 3 0 R",
            predictor);

        var metadata = PdfInfoReader.TryRead(new MemoryStream(pdf));

        Assert.Equal("Programming = (++)Algorithms;", metadata!.Title);
        Assert.Equal(["Преслав Наков", "Панайот Добриков"], metadata.Authors);
    }

    [Fact]
    public void AnIncrementalUpdate_GivesTheNewestInformation_AndIndirectStringsAreFollowed()
    {
        var original = RawPdf.Classic(
            new() { [1] = Catalog, [2] = Pages, [3] = "<< /Title (Old Title) /Author (Old Author) >>" },
            "/Root 1 0 R /Info 3 0 R");
        var updated = RawPdf.Update(original, new() { [3] = "<< /Title 4 0 R /Author (New Author) >>", [4] = "(New \\101 Title)" }, "/Root 1 0 R /Info 3 0 R");

        var metadata = PdfInfoReader.TryRead(new MemoryStream(updated));

        Assert.Equal("New A Title", metadata!.Title);
        Assert.Equal(["New Author"], metadata.Authors);
    }

    [Fact]
    public void InformationWithoutATitle_DoesNotTakeOneFromTheNextObject()
    {
        // Seen in a real file: the next object, a bookmark, has a /Title of its own.
        var pdf = RawPdf.Classic(
            new() { [1] = Catalog, [2] = Pages, [3] = "<< /Creator (Microsoft Word) >>", [4] = "<< /Title (Purpose of the Guide) /Next 5 0 R >>" },
            "/Root 1 0 R /Info 3 0 R");

        var metadata = PdfInfoReader.TryRead(new MemoryStream(pdf));

        Assert.Null(metadata!.Title);
    }

    [Fact]
    public void WithoutInformation_TheTitleIsLeftToTheFileName()
    {
        var pdf = RawPdf.Classic(new() { [1] = Catalog, [2] = Pages }, "/Root 1 0 R");

        var metadata = PdfInfoReader.TryRead(new MemoryStream(pdf));

        Assert.Null(metadata!.Title);
        Assert.Empty(metadata.Authors);
    }

    [Fact]
    public void Encrypted_OrNotAPdf_GivesNone_SoTheWholeFileIsRead()
    {
        var encrypted = RawPdf.Classic(
            new() { [1] = Catalog, [2] = Pages, [3] = "<< /Title (x) >>", [4] = "<< /Filter /Standard >>" },
            "/Root 1 0 R /Info 3 0 R /Encrypt 4 0 R");

        Assert.Null(PdfInfoReader.TryRead(new MemoryStream(encrypted)));
        Assert.Null(PdfInfoReader.TryRead(new MemoryStream(Encoding.ASCII.GetBytes("not a pdf at all"))));
        Assert.Null(PdfInfoReader.TryRead(new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4\nstartxref\n99999\n%%EOF"))));
    }

    private sealed class CountingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public long BytesRead { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = base.Read(buffer, offset, count);
            BytesRead += n;
            return n;
        }
    }
}
