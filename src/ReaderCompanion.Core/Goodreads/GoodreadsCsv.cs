using System.Globalization;
using System.Text;
using ReaderCompanion.Core.Library;
using ReaderCompanion.Core.Metadata;

namespace ReaderCompanion.Core.Goodreads;

/// <summary>One book of a Goodreads library export. <see cref="Status"/> is null for shelves that are not a reading
/// status; <see cref="Rating"/> is null when not rated; <see cref="Shelves"/> are the user's own shelves only.</summary>
public sealed record GoodreadsRow(
    string GoodreadsId,
    string Title,
    IReadOnlyList<string> Authors,
    string? Isbn,
    int? Rating,
    DateOnly? DateRead,
    DateOnly? DateAdded,
    ReadStatus? Status,
    IReadOnlyList<string> Shelves)
{
    /// <summary>The series Goodreads appends to the title, e.g. "Mistborn (Mistborn, #1)".</summary>
    public string? Series { get; init; }

    public decimal? SeriesIndex { get; init; }
}

/// <summary>Reads Goodreads' "Export library" CSV.</summary>
public static partial class GoodreadsCsv
{
    private static readonly string[] Required = ["Book Id", "Title", "Author", "Exclusive Shelf"];

    // Goodreads' own shelves are reading statuses (see Exclusive Shelf), not the user's shelves.
    private static readonly HashSet<string> StatusShelves = new(StringComparer.OrdinalIgnoreCase) { "read", "currently-reading", "to-read" };

    /// <exception cref="InvalidDataException">The text is not a Goodreads library export.</exception>
    public static IReadOnlyList<GoodreadsRow> Parse(TextReader reader)
    {
        using var records = Records(reader).GetEnumerator();
        if (!records.MoveNext())
        {
            throw new InvalidDataException("The file is empty, not a Goodreads library export.");
        }

        var columns = records.Current
            .Select((name, index) => (Name: name.Trim(), Index: index))
            .GroupBy(c => c.Name)
            .ToDictionary(g => g.Key, g => g.First().Index);
        if (Required.Any(name => !columns.ContainsKey(name)))
        {
            throw new InvalidDataException("This is not a Goodreads library export (its columns are missing).");
        }

        var rows = new List<GoodreadsRow>();
        while (records.MoveNext())
        {
            var record = records.Current;
            string? Field(string name) =>
                columns.TryGetValue(name, out var i) && i < record.Count ? NameNormalizer.Clean(Unwrap(record[i])) : null;

            if (Field("Book Id") is not { } id || Field("Title") is not { } title)
            {
                continue; // a blank or broken line
            }

            var series = SeriesSuffix().Match(title);
            rows.Add(new GoodreadsRow(
                id,
                series.Success ? series.Groups["title"].Value : title,
                [.. new[] { Field("Author") }.Concat((Field("Additional Authors") ?? "").Split(','))
                    .Select(NameNormalizer.Clean).OfType<string>()],
                Isbn(Field("ISBN13")) ?? Isbn(Field("ISBN")),
                int.TryParse(Field("My Rating"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stars) && stars is >= 1 and <= 5 ? stars : null,
                Date(Field("Date Read")),
                Date(Field("Date Added")),
                Field("Exclusive Shelf") switch
                {
                    "read" => ReadStatus.Read,
                    "currently-reading" => ReadStatus.Reading,
                    "to-read" => ReadStatus.WantToRead,
                    _ => null,
                },
                [.. (Field("Bookshelves") ?? "").Split(',').Select(NameNormalizer.Clean).OfType<string>().Where(s => !StatusShelves.Contains(s))])
            {
                Series = series.Success ? series.Groups["series"].Value : null,
                SeriesIndex = series.Success ? decimal.Parse(series.Groups["n"].Value, CultureInfo.InvariantCulture) : null,
            });
        }

        return rows;
    }

    /// <summary>"Title (Series, #2)" or "Title (Series #2.5)".</summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"^(?<title>.+?)\s*\((?<series>[^()]+?),?\s+#(?<n>\d+(?:\.\d+)?)\)$")]
    private static partial System.Text.RegularExpressions.Regex SeriesSuffix();

    /// <summary>Goodreads writes ISBNs as <c>="9780552131063"</c> so spreadsheets keep the leading zeros.</summary>
    private static string Unwrap(string value) =>
        value.StartsWith("=\"", StringComparison.Ordinal) && value.EndsWith('"') ? value[2..^1] : value;

    private static string? Isbn(string? value) => MatchKeys.Isbn(value) is { Length: 10 or 13 } isbn ? isbn : null;

    private static DateOnly? Date(string? value) =>
        DateOnly.TryParseExact(value, ["yyyy/MM/dd", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    /// <summary>RFC 4180 records: quoted fields may hold commas, doubled quotes and newlines.</summary>
    private static IEnumerable<IReadOnlyList<string>> Records(TextReader reader)
    {
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var any = false;
        int c;
        while ((c = reader.Read()) != -1)
        {
            any = true;
            var ch = (char)c;
            if (quoted)
            {
                if (ch == '"' && reader.Peek() == '"')
                {
                    field.Append('"');
                    reader.Read();
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(ch);
                }
            }
            else if (ch == '"' && field.Length == 0)
            {
                quoted = true;
            }
            else if (ch == ',')
            {
                record.Add(field.ToString());
                field.Clear();
            }
            else if (ch is '\n' or '\r')
            {
                if (ch == '\r' && reader.Peek() == '\n')
                {
                    reader.Read();
                }

                record.Add(field.ToString());
                field.Clear();
                yield return record;
                record = [];
                any = false;
            }
            else
            {
                field.Append(ch);
            }
        }

        if (any)
        {
            record.Add(field.ToString());
            yield return record;
        }
    }
}
