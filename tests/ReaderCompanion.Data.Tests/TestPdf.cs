using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace ReaderCompanion.Data.Tests;

/// <summary>Builds PDFs in memory, as large as needed, with a title and author.</summary>
internal static class TestPdf
{
    public static byte[] Build(string title, string author, int pages = 1)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        for (var i = 0; i < pages; i++)
        {
            var page = builder.AddPage(PageSize.A4);
            for (var line = 0; line < 40; line++)
            {
                page.AddText($"Page {i}, line {line}: some text to give the page weight.", 10, new UglyToad.PdfPig.Core.PdfPoint(40, 800 - (line * 18)), font);
            }
        }

        builder.DocumentInformation.Title = title;
        builder.DocumentInformation.Author = author;
        return builder.Build();
    }
}
