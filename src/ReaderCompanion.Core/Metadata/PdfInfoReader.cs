using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Reads a PDF's title and author from as little of the file as possible: the trailer at its end, the
/// cross-reference data that points to the document information, and that dictionary. Meant for a file read in place
/// over a network, where a full PDF parser would touch every page. Follows classic cross-reference tables,
/// cross-reference streams (Flate, with or without a PNG predictor), object streams and incremental updates.</summary>
public static partial class PdfInfoReader
{
    private const int TailLength = 2048;
    private const int MaxSections = 32;

    /// <summary>The title and authors, or null when the PDF is laid out in a way this does not follow (encrypted,
    /// damaged, an unusual filter): the caller then reads the whole file. Read failures of the stream itself
    /// (<see cref="IOException"/>) are not caught.</summary>
    public static BookMetadata? TryRead(Stream pdf)
    {
        try
        {
            return new Reader(pdf).ReadInfo() is { } info
                ? new BookMetadata { Title = NameNormalizer.Clean(info.Title), Authors = PdfMetadataReader.Authors(info.Author) }
                : null;
        }
        catch (Exception e) when (e is InvalidDataException or FormatException or OverflowException or ArgumentException
                                       or IndexOutOfRangeException or InvalidOperationException)
        {
            return null;
        }
    }

    private sealed partial class Reader(Stream pdf)
    {
        private readonly List<Section> _sections = [];

        public (string? Title, string? Author)? ReadInfo()
        {
            var length = pdf.Length;
            var tailStart = Math.Max(0, length - TailLength);
            var tail = Text(tailStart, (int)(length - tailStart));
            var startxref = tail.LastIndexOf("startxref", StringComparison.Ordinal);
            if (startxref < 0)
            {
                return null;
            }

            LoadChain(long.Parse(Integer().Match(tail, startxref + 9).Groups[1].Value, CultureInfo.InvariantCulture));
            if (_sections.Any(s => s.Trailer.Contains("/Encrypt", StringComparison.Ordinal)))
            {
                return null; // strings are encrypted
            }

            if (_sections.Select(s => Reference(s.Trailer, "Info")).FirstOrDefault(r => r is not null) is not { } info)
            {
                return (null, null); // no document information at all: a valid answer
            }

            var dictionary = Dictionary(Object(info), 0); // only this dictionary: the next object may have a /Title too
            return (StringValue(dictionary, "Title"), StringValue(dictionary, "Author"));
        }

        // ---------- cross-reference sections ----------

        private sealed record Section(string Trailer, Func<int, Location?> Find);

        private abstract record Location;

        private sealed record AtOffset(long Offset) : Location;

        private sealed record InStream(int Stream, int Index) : Location;

        private void LoadChain(long offset)
        {
            var seen = new HashSet<long>();
            var next = new Queue<long>([offset]);
            while (next.TryDequeue(out var at) && seen.Add(at) && _sections.Count < MaxSections)
            {
                var section = Text(at, 16).TrimStart().StartsWith("xref", StringComparison.Ordinal) ? Table(at) : XrefStream(at);
                _sections.Add(section);
                if (Number(section.Trailer, "XRefStm") is { } hybrid)
                {
                    next.Enqueue(hybrid); // a classic table with a stream for compressed objects
                }

                if (Number(section.Trailer, "Prev") is { } previous)
                {
                    next.Enqueue(previous);
                }
            }
        }

        /// <summary>A classic table: subsection headers are read, entries are only read when looked up.</summary>
        private Section Table(long offset)
        {
            var subsections = new List<(int Start, int Count, long At, int Width)>();
            var at = offset + Text(offset, 16).IndexOf("xref", StringComparison.Ordinal) + 4;
            while (true)
            {
                var text = Text(at, 64);
                var skip = text.Length - text.TrimStart().Length;
                at += skip;
                text = text[skip..];
                if (text.StartsWith("trailer", StringComparison.Ordinal))
                {
                    return new Section(Dictionary(Text(at, 8192), 0), number => Entry(subsections, number));
                }

                var header = SubsectionHeader().Match(text);
                if (!header.Success)
                {
                    throw new InvalidDataException("Damaged cross-reference table.");
                }

                var (start, count) = (int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(header.Groups[2].Value, CultureInfo.InvariantCulture));
                at += header.Length;
                var first = Text(at, 21);
                var width = first.Length > 19 && first[18] == '\n' && char.IsAsciiDigit(first[19]) ? 19 : 20; // some writers end entries with one byte
                subsections.Add((start, count, at, width));
                at += (long)count * width;
            }
        }

        private Location? Entry(List<(int Start, int Count, long At, int Width)> subsections, int number)
        {
            foreach (var (start, count, at, width) in subsections)
            {
                if (number >= start && number < start + count)
                {
                    var entry = Text(at + ((long)(number - start) * width), 18);
                    return entry.Length >= 18 && entry[17] == 'n'
                        ? new AtOffset(long.Parse(entry[..10], CultureInfo.InvariantCulture))
                        : null;
                }
            }

            return null;
        }

        private Section XrefStream(long offset)
        {
            var (dictionary, data) = StreamObject(offset);
            if (!dictionary.Contains("/XRef", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Not a cross-reference stream.");
            }

            var widths = Array(dictionary, "W") ?? throw new InvalidDataException("No /W.");
            var index = Array(dictionary, "Index") ?? [0, Number(dictionary, "Size") ?? 0];
            var row = (int)widths.Sum();
            return new Section(dictionary, number =>
            {
                long rowStart = 0;
                for (var i = 0; i + 1 < index.Length; i += 2)
                {
                    if (number >= index[i] && number < index[i] + index[i + 1])
                    {
                        var at = (int)((rowStart + number - index[i]) * row);
                        if (at + row > data.Length)
                        {
                            return null;
                        }

                        long Field(int f, int from) => Enumerable.Range(from, (int)widths[f]).Aggregate(0L, (v, b) => (v << 8) | data[at + b]);
                        var type = widths[0] == 0 ? 1 : Field(0, 0);
                        var second = Field(1, (int)widths[0]);
                        var third = Field(2, (int)(widths[0] + widths[1]));
                        return type switch
                        {
                            1 => new AtOffset(second),
                            2 => new InStream((int)second, (int)third),
                            _ => null,
                        };
                    }

                    rowStart += index[i + 1];
                }

                return null;
            });
        }

        private Location Find(int number) =>
            _sections.Select(s => s.Find(number)).FirstOrDefault(l => l is not null)
            ?? throw new InvalidDataException($"Object {number} is not in the cross-reference data.");

        // ---------- objects ----------

        /// <summary>An object's text (a dictionary or a string), wherever it lives.</summary>
        private string Object(int number)
        {
            switch (Find(number))
            {
                case AtOffset { Offset: var offset }:
                    var text = Text(offset, 16384);
                    var header = ObjectHeader().Match(text);
                    return header.Success ? text[header.Length..] : throw new InvalidDataException("No object header.");
                case InStream { Stream: var stream, Index: var index }:
                    if (Find(stream) is not AtOffset { Offset: var at })
                    {
                        throw new InvalidDataException("Object stream inside an object stream.");
                    }

                    var (dictionary, data) = StreamObject(at);
                    var content = Encoding.Latin1.GetString(data);
                    var first = (int)(Number(dictionary, "First") ?? throw new InvalidDataException("No /First."));
                    var pairs = content[..first].Split((char[])[' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i + 1 < pairs.Length; i += 2)
                    {
                        if (int.Parse(pairs[i], CultureInfo.InvariantCulture) == number)
                        {
                            return content[(first + int.Parse(pairs[i + 1], CultureInfo.InvariantCulture))..];
                        }
                    }

                    throw new InvalidDataException("Object not in its object stream.");
                default:
                    throw new InvalidDataException("Unknown location.");
            }
        }

        /// <summary>A stream object's dictionary and its decoded data (Flate, PNG predictors).</summary>
        private (string Dictionary, byte[] Data) StreamObject(long offset)
        {
            var text = Text(offset, 8192);
            var header = ObjectHeader().Match(text);
            if (!header.Success)
            {
                throw new InvalidDataException("No object header.");
            }

            var dictionary = Dictionary(text, header.Length);
            var keyword = text.IndexOf("stream", text.IndexOf(dictionary, StringComparison.Ordinal) + dictionary.Length, StringComparison.Ordinal);
            var start = keyword + 6 + (text[keyword + 6] == '\r' ? 2 : 1);
            var length = Number(dictionary, "Length") is { } l && Reference(dictionary, "Length") is null
                ? (int)l
                : throw new InvalidDataException("Stream length is not given directly.");
            var raw = Bytes(offset + start, length);
            var filter = Name(dictionary, "Filter");
            var data = filter switch
            {
                null => raw,
                "FlateDecode" => Inflate(raw),
                _ => throw new InvalidDataException($"Unsupported filter {filter}."),
            };
            return (dictionary, Number(dictionary, "Predictor") is >= 10 ? Unpredict(data, (int)(Number(dictionary, "Columns") ?? 1)) : data);
        }

        private static byte[] Inflate(byte[] raw)
        {
            using var input = new ZLibStream(new MemoryStream(raw), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }

        /// <summary>Undoes PNG row predictors (each row starts with its filter type).</summary>
        private static byte[] Unpredict(byte[] data, int columns)
        {
            var rows = data.Length / (columns + 1);
            var result = new byte[rows * columns];
            for (var r = 0; r < rows; r++)
            {
                var filter = data[r * (columns + 1)];
                for (var c = 0; c < columns; c++)
                {
                    var value = data[(r * (columns + 1)) + 1 + c];
                    var left = c > 0 ? result[(r * columns) + c - 1] : 0;
                    var up = r > 0 ? result[((r - 1) * columns) + c] : 0;
                    var upLeft = r > 0 && c > 0 ? result[((r - 1) * columns) + c - 1] : 0;
                    result[(r * columns) + c] = filter switch
                    {
                        0 => value,
                        1 => (byte)(value + left),
                        2 => (byte)(value + up),
                        3 => (byte)(value + ((left + up) / 2)),
                        4 => (byte)(value + Paeth(left, up, upLeft)),
                        _ => throw new InvalidDataException("Unknown PNG predictor."),
                    };
                }
            }

            return result;
        }

        private static int Paeth(int a, int b, int c)
        {
            var p = a + b - c;
            var (pa, pb, pc) = (Math.Abs(p - a), Math.Abs(p - b), Math.Abs(p - c));
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        // ---------- values ----------

        /// <summary>The text of the dictionary starting at the first "&lt;&lt;" from <paramref name="from"/>, with
        /// nested dictionaries and strings (which may hold brackets) skipped over.</summary>
        private static string Dictionary(string text, int from)
        {
            var start = text.IndexOf("<<", from, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidDataException("No dictionary.");
            }

            var depth = 0;
            for (var i = start; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '(':
                        i = EndOfLiteral(text, i);
                        break;
                    case '<' when i + 1 < text.Length && text[i + 1] == '<':
                        depth++;
                        i++;
                        break;
                    case '>' when i + 1 < text.Length && text[i + 1] == '>':
                        depth--;
                        i++;
                        if (depth == 0)
                        {
                            return text[start..(i + 1)];
                        }

                        break;
                }
            }

            throw new InvalidDataException("Unfinished dictionary.");
        }

        private static int EndOfLiteral(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\\':
                        i++;
                        break;
                    case '(':
                        depth++;
                        break;
                    case ')' when --depth == 0:
                        return i;
                }
            }

            throw new InvalidDataException("Unfinished string.");
        }

        /// <summary>The value text right after /Key, or null without the key.</summary>
        private static string? Value(string dictionary, string key)
        {
            var match = Regex.Match(dictionary, $@"/{key}(?=[\s/<\[(])\s*");
            return match.Success ? dictionary[(match.Index + match.Length)..] : null;
        }

        private static long? Number(string dictionary, string key) =>
            Value(dictionary, key) is { } value && Integer().Match(value) is { Success: true, Index: 0 } m
                ? long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
                : null;

        private static int? Reference(string dictionary, string key) =>
            Value(dictionary, key) is { } value && IndirectReference().Match(value) is { Success: true, Index: 0 } m
                ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
                : null;

        private static string? Name(string dictionary, string key) =>
            Value(dictionary, key) is { } value && NameValue().Match(value) is { Success: true, Index: 0 } m ? m.Groups[1].Value : null;

        private static long[]? Array(string dictionary, string key) =>
            Value(dictionary, key) is { } value && value.StartsWith('[')
                ? [.. value[1..value.IndexOf(']', StringComparison.Ordinal)]
                    .Split((char[])[' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => long.Parse(n, CultureInfo.InvariantCulture))]
                : null;

        /// <summary>A text string value, following an indirect reference to it.</summary>
        private string? StringValue(string dictionary, string key)
        {
            if (Reference(dictionary, key) is { } number)
            {
                return StringValue("/V " + Object(number).TrimStart(), "V");
            }

            var value = Value(dictionary, key);
            if (value is null || value.Length == 0)
            {
                return null;
            }

            var bytes = value[0] switch
            {
                '(' => Literal(value[..(EndOfLiteral(value, 0) + 1)]),
                '<' when value.Length > 1 && value[1] != '<' => Hex(value[1..value.IndexOf('>', StringComparison.Ordinal)]),
                _ => null,
            };
            return bytes is null ? null : Decode(bytes);
        }

        private static byte[] Literal(string literal)
        {
            var bytes = new List<byte>();
            for (var i = 1; i < literal.Length - 1; i++)
            {
                var c = literal[i];
                if (c != '\\')
                {
                    bytes.Add((byte)c);
                    continue;
                }

                var e = literal[++i];
                switch (e)
                {
                    case 'n': bytes.Add((byte)'\n'); break;
                    case 'r': bytes.Add((byte)'\r'); break;
                    case 't': bytes.Add((byte)'\t'); break;
                    case 'b': bytes.Add((byte)'\b'); break;
                    case 'f': bytes.Add((byte)'\f'); break;
                    case '\r':
                        if (i + 1 < literal.Length - 1 && literal[i + 1] == '\n')
                        {
                            i++;
                        }

                        break;
                    case '\n': break; // line continuation
                    case >= '0' and <= '7':
                        var digits = 1;
                        while (digits < 3 && i + 1 < literal.Length - 1 && literal[i + 1] is >= '0' and <= '7')
                        {
                            i++;
                            digits++;
                        }

                        bytes.Add((byte)Convert.ToInt32(literal.Substring(i - digits + 1, digits), 8));
                        break;
                    default: bytes.Add((byte)e); break; // \( \) \\ and unknown escapes
                }
            }

            return [.. bytes];
        }

        private static byte[] Hex(string hex)
        {
            var digits = new string([.. hex.Where(char.IsAsciiHexDigit)]);
            if (digits.Length % 2 == 1)
            {
                digits += "0";
            }

            return Convert.FromHexString(digits);
        }

        /// <summary>UTF-16BE or UTF-8 with their byte order marks, otherwise PDFDocEncoding (read as Latin-1).</summary>
        private static string Decode(byte[] bytes) => bytes switch
        {
            [0xFE, 0xFF, ..] => Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2),
            [0xEF, 0xBB, 0xBF, ..] => Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3),
            _ => Encoding.Latin1.GetString(bytes),
        };

        // ---------- bytes ----------

        private string Text(long offset, int count) => Encoding.Latin1.GetString(Bytes(offset, count, exact: false));

        private byte[] Bytes(long offset, int count, bool exact = true)
        {
            if (offset < 0 || offset > pdf.Length)
            {
                throw new InvalidDataException("Offset outside the file.");
            }

            count = (int)Math.Min(count, pdf.Length - offset);
            var buffer = new byte[count];
            pdf.Position = offset;
            var read = 0;
            while (read < count && pdf.Read(buffer, read, count - read) is var n and > 0)
            {
                read += n;
            }

            return read == count ? buffer : exact ? throw new InvalidDataException("File ends early.") : buffer[..read];
        }

        [GeneratedRegex(@"\G\s*(\d+)")]
        private static partial Regex Integer();

        [GeneratedRegex(@"\A(\d+)\s+(\d+)[ \t]*\r?\n?")]
        private static partial Regex SubsectionHeader();

        [GeneratedRegex(@"\A\s*\d+\s+\d+\s+obj\b")]
        private static partial Regex ObjectHeader();

        [GeneratedRegex(@"\A(\d+)\s+\d+\s+R\b")]
        private static partial Regex IndirectReference();

        [GeneratedRegex(@"\A/([^\s/<>\[\]()]+)")]
        private static partial Regex NameValue();
    }
}
