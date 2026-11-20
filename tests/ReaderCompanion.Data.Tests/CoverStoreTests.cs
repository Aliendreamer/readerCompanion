using Microsoft.Extensions.DependencyInjection;

namespace ReaderCompanion.Data.Tests;

public sealed class CoverStoreTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private string CoversFolder => Path.Combine(Path.GetDirectoryName(_library.DatabasePath)!, "covers");

    private CoverStore Store => _library.Services.GetRequiredService<CoverStore>();

    [Theory]
    [InlineData("abc.jpg")]
    [InlineData("abc.jpeg")]
    [InlineData("abc.PNG")]
    [InlineData("abc.gif")]
    [InlineData("abc.webp")]
    public void PathFor_ExistingCover_GivesItsPathInTheCoversFolder(string fileName)
    {
        Directory.CreateDirectory(CoversFolder);
        File.WriteAllBytes(Path.Combine(CoversFolder, fileName), [1, 2, 3]);

        Assert.Equal(Path.Combine(CoversFolder, fileName), Store.PathFor(fileName));
    }

    [Theory]
    [InlineData("../library.db")]
    [InlineData("..")]
    [InlineData("sub/abc.jpg")]
    [InlineData("sub\\abc.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    [InlineData("abc.txt")]
    public void PathFor_RefusesNamesOutsideTheCoversFolder_OrNotImages(string fileName)
    {
        Directory.CreateDirectory(CoversFolder);
        File.WriteAllBytes(Path.Combine(CoversFolder, "abc.txt"), [1]);

        Assert.Null(Store.PathFor(fileName));
    }

    [Fact]
    public void PathFor_MissingFile_GivesNull() => Assert.Null(Store.PathFor("nope.jpg"));

    public void Dispose() => _library.Dispose();
}
