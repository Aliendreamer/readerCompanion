using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace ReaderCompanion.Core.Tests;

/// <summary>Writes small PDFs by hand, in the layouts real files use: a classic cross-reference table, or a
/// cross-reference stream with objects packed in an object stream; and incremental updates.</summary>
internal static class RawPdf
{
    public static byte[] Classic(Dictionary<int, string> objects, string trailer)
    {
        using var pdf = new MemoryStream();
        Write(pdf, "%PDF-1.4\n");
        var offsets = WriteObjects(pdf, objects);
        WriteTable(pdf, offsets, objects.Keys.Max() + 1, trailer);
        return pdf.ToArray();
    }

    /// <summary>Appends changed objects and a new table pointing back with /Prev.</summary>
    public static byte[] Update(byte[] original, Dictionary<int, string> objects, string trailer)
    {
        var text = Encoding.Latin1.GetString(original);
        var startxref = text[(text.LastIndexOf("startxref", StringComparison.Ordinal) + 9)..].Trim().Split('\n')[0].Trim();
        using var pdf = new MemoryStream();
        pdf.Write(original);
        var offsets = WriteObjects(pdf, objects);
        WriteTable(pdf, offsets, objects.Keys.Max() + 1, $"{trailer} /Prev {startxref}");
        return pdf.ToArray();
    }

    public static byte[] WithXrefStream(Dictionary<int, string> direct, Dictionary<int, string> packed, string trailer, bool predictor)
    {
        using var pdf = new MemoryStream();
        Write(pdf, "%PDF-1.5\n");
        var offsets = WriteObjects(pdf, direct);
        var stream = Math.Max(direct.Keys.DefaultIfEmpty(0).Max(), packed.Keys.DefaultIfEmpty(0).Max()) + 1;
        var xref = stream + 1;

        var header = new StringBuilder();
        var body = new StringBuilder();
        foreach (var (number, content) in packed)
        {
            header.Append(CultureInfo.InvariantCulture, $"{number} {body.Length} ");
            body.Append(content).Append(' ');
        }

        var packedBytes = Encoding.Latin1.GetBytes(header.ToString() + body);
        offsets[stream] = pdf.Position;
        WriteStream(pdf, stream, $"/Type /ObjStm /N {packed.Count} /First {header.Length}", Deflate(packedBytes));

        var rows = new List<byte[]>();
        var index = packed.Keys.Select((n, i) => (n, i)).ToDictionary(p => p.n, p => p.i);
        offsets[xref] = pdf.Position;
        for (var n = 0; n <= xref; n++)
        {
            rows.Add(offsets.TryGetValue(n, out var at) ? Row(1, at, 0)
                : index.TryGetValue(n, out var i) ? Row(2, stream, i)
                : Row(0, 0, 0));
        }

        var data = predictor ? Up(rows) : rows.SelectMany(r => r).ToArray();
        WriteStream(pdf, xref,
            $"/Type /XRef /Size {xref + 1} /W [1 4 2] {(predictor ? "/DecodeParms << /Columns 7 /Predictor 12 >>" : "")} {trailer}",
            Deflate(data));
        Write(pdf, $"startxref\n{offsets[xref]}\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] Row(int type, long second, int third) =>
        [(byte)type, (byte)(second >> 24), (byte)(second >> 16), (byte)(second >> 8), (byte)second, (byte)(third >> 8), (byte)third];

    /// <summary>PNG "Up" predictor: each row is prefixed with 2 and holds the difference from the row above.</summary>
    private static byte[] Up(List<byte[]> rows)
    {
        var result = new List<byte>();
        for (var r = 0; r < rows.Count; r++)
        {
            result.Add(2);
            result.AddRange(rows[r].Select((b, c) => (byte)(b - (r > 0 ? rows[r - 1][c] : 0))));
        }

        return [.. result];
    }

    private static Dictionary<int, long> WriteObjects(MemoryStream pdf, Dictionary<int, string> objects)
    {
        var offsets = new Dictionary<int, long>();
        foreach (var (number, body) in objects)
        {
            offsets[number] = pdf.Position;
            Write(pdf, $"{number} 0 obj\n{body}\nendobj\n");
        }

        return offsets;
    }

    private static void WriteTable(MemoryStream pdf, Dictionary<int, long> offsets, int size, string trailer)
    {
        var at = pdf.Position;
        var table = new StringBuilder("xref\n");
        // One subsection per run of written objects, as updates write them.
        foreach (var number in offsets.Keys.Order())
        {
            table.Append(CultureInfo.InvariantCulture, $"{number} 1\n{offsets[number]:D10} 00000 n\r\n");
        }

        Write(pdf, $"{table}trailer\n<< /Size {size} {trailer} >>\nstartxref\n{at}\n%%EOF\n");
    }

    private static void WriteStream(MemoryStream pdf, int number, string dictionary, byte[] data)
    {
        Write(pdf, $"{number} 0 obj\n<< {dictionary} /Length {data.Length} /Filter /FlateDecode >>\nstream\n");
        pdf.Write(data);
        Write(pdf, "\nendstream\nendobj\n");
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var z = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(data);
        }

        return output.ToArray();
    }

    private static void Write(MemoryStream pdf, string text) => pdf.Write(Encoding.Latin1.GetBytes(text));
}
