using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ReaderCompanion.Core.Metadata;

/// <summary>Reads book details from an EPUB's package (OPF) metadata. Only the OPF and the cover are read.</summary>
public static class EpubMetadataReader
{
    /// <summary>Covers larger than this are ignored (protects against huge or malicious entries).</summary>
    public const int MaxCoverBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
    };

    public static BookMetadata Read(Stream epub)
    {
        using var zip = new ZipArchive(epub, ZipArchiveMode.Read, leaveOpen: true);

        var opfPath = LoadXml(zip, "META-INF/container.xml")
            .Descendants().First(e => e.Name.LocalName == "rootfile")
            .Attribute("full-path")!.Value;
        var opf = LoadXml(zip, opfPath).Root!;
        var metadata = Child(opf, "metadata");
        var manifest = Child(opf, "manifest");

        var dc = (string name) => metadata?.Elements().Where(e => e.Name.LocalName == name) ?? [];
        var metas = metadata?.Elements().Where(e => e.Name.LocalName == "meta").ToList() ?? [];

        var (series, seriesIndex) = ReadSeries(metas);
        return new BookMetadata
        {
            Title = dc("title").Select(e => NameNormalizer.Clean(e.Value)).FirstOrDefault(v => v is not null),
            Authors = Values(dc("creator")),
            Language = dc("language").Select(e => NameNormalizer.Clean(e.Value)).FirstOrDefault(v => v is not null),
            Isbn = dc("identifier").Select(ReadIsbn).FirstOrDefault(v => v is not null),
            Genres = Values(dc("subject")),
            Series = series,
            SeriesIndex = seriesIndex,
            Cover = ReadCover(zip, opfPath, metas, manifest),
        };
    }

    private static (string? Series, decimal? Index) ReadSeries(List<XElement> metas)
    {
        // EPUB 3: <meta property="belongs-to-collection" id="x">Name</meta> + <meta refines="#x" property="group-position">
        var collection = metas.FirstOrDefault(m => (string?)m.Attribute("property") == "belongs-to-collection"
            && NameNormalizer.Clean(m.Value) is not null);
        if (collection is not null)
        {
            var id = (string?)collection.Attribute("id");
            var position = metas.FirstOrDefault(m => (string?)m.Attribute("property") == "group-position"
                && id is not null && (string?)m.Attribute("refines") == "#" + id);
            return (NameNormalizer.Clean(collection.Value), ParseIndex(position?.Value));
        }

        // Calibre: <meta name="calibre:series" content="Name"/> + <meta name="calibre:series_index" content="2.0"/>
        string? Named(string name) => metas.Where(m => (string?)m.Attribute("name") == name)
            .Select(m => (string?)m.Attribute("content")).FirstOrDefault();
        var calibreSeries = NameNormalizer.Clean(Named("calibre:series"));
        return calibreSeries is null ? (null, null) : (calibreSeries, ParseIndex(Named("calibre:series_index")));
    }

    private static CoverImage? ReadCover(ZipArchive zip, string opfPath, List<XElement> metas, XElement? manifest)
    {
        var items = manifest?.Elements().Where(e => e.Name.LocalName == "item").ToList() ?? [];

        var item = items.FirstOrDefault(i => ((string?)i.Attribute("properties") ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("cover-image"));
        if (item is null)
        {
            var coverId = metas.Where(m => (string?)m.Attribute("name") == "cover")
                .Select(m => (string?)m.Attribute("content")).FirstOrDefault();
            item = items.FirstOrDefault(i => coverId is not null && (string?)i.Attribute("id") == coverId);
        }

        var href = (string?)item?.Attribute("href");
        if (href is null)
        {
            return null;
        }

        // Manifest hrefs are URLs relative to the OPF file (may contain ../ and %-escapes).
        var resolved = new Uri(new Uri("epub:///" + opfPath), href);
        var entryPath = Uri.UnescapeDataString(resolved.AbsolutePath).TrimStart('/');
        var entry = zip.GetEntry(entryPath);
        if (entry is null || entry.Length > MaxCoverBytes)
        {
            return null;
        }

        using var stream = entry.Open();
        using var bytes = new MemoryStream((int)entry.Length);
        stream.CopyTo(bytes);
        return new CoverImage(bytes.ToArray(), CoverExtension(entryPath, (string?)item!.Attribute("media-type")));
    }

    /// <summary>A safe file extension: the entry's own if it is a known image type, else from the media type.</summary>
    private static string CoverExtension(string entryPath, string? mediaType)
    {
        var own = Path.GetExtension(entryPath).ToLowerInvariant();
        if (own == ".jpeg")
        {
            own = ".jpg";
        }

        return ImageExtensions.ContainsValue(own) ? own
            : mediaType is not null && ImageExtensions.TryGetValue(mediaType, out var fromType) ? fromType
            : ".jpg";
    }

    private static string? ReadIsbn(XElement identifier)
    {
        var value = identifier.Value.Trim();
        if (value.StartsWith("urn:isbn:", StringComparison.OrdinalIgnoreCase))
        {
            value = value["urn:isbn:".Length..];
        }

        var compact = new string(value.Where(c => c is not ('-' or ' ')).ToArray()).ToUpperInvariant();
        var looksLikeIsbn = (compact.Length == 13 && compact.All(char.IsAsciiDigit))
            || (compact.Length == 10 && compact[..9].All(char.IsAsciiDigit) && (char.IsAsciiDigit(compact[9]) || compact[9] == 'X'));
        return looksLikeIsbn ? compact : null;
    }

    private static decimal? ParseIndex(string? value) =>
        decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var index) ? index : null;

    private static List<string> Values(IEnumerable<XElement> elements) =>
        elements.Select(e => NameNormalizer.Clean(e.Value)).OfType<string>().ToList();

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static XDocument LoadXml(ZipArchive zip, string path)
    {
        var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"EPUB is missing '{path}'.");
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }
}
