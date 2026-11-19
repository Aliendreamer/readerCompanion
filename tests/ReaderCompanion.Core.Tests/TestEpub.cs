using System.IO.Compression;
using System.Text;

namespace ReaderCompanion.Core.Tests;

/// <summary>Builds minimal EPUB files in memory: container.xml pointing at OEBPS/content.opf.</summary>
internal static class TestEpub
{
    public static MemoryStream Build(string metadata, string manifest = "", params (string Path, byte[] Bytes)[] files)
    {
        var opf = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <package xmlns="http://www.idpf.org/2007/opf" xmlns:opf="http://www.idpf.org/2007/opf" version="3.0" unique-identifier="id">
              <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">{metadata}</metadata>
              <manifest>{manifest}</manifest>
              <spine/>
            </package>
            """;

        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "mimetype", "application/epub+zip");
            Add(zip, "META-INF/container.xml", """
                <?xml version="1.0"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles><rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/></rootfiles>
                </container>
                """);
            Add(zip, "OEBPS/content.opf", opf);
            foreach (var (path, bytes) in files)
            {
                using var entry = zip.CreateEntry(path).Open();
                entry.Write(bytes);
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        using var entry = zip.CreateEntry(path).Open();
        entry.Write(Encoding.UTF8.GetBytes(content));
    }
}
