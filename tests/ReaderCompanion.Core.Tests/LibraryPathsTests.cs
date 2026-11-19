using ReaderCompanion.Core.Scanning;

namespace ReaderCompanion.Core.Tests;

public class LibraryPathsTests
{
    [Theory]
    [InlineData("/books", "/books", true)]
    [InlineData("/books/fantasy/a.epub", "/books", true)]
    [InlineData("/books/", "/books", true)]
    [InlineData("/bookshelf/a.epub", "/books", false)]
    [InlineData("/other", "/books", false)]
    public void IsSameOrUnder(string path, string root, bool expected) =>
        Assert.Equal(expected, LibraryPaths.IsSameOrUnder(path, root));

    [Fact]
    public void IsSameOrUnder_CaseFollowsPlatform()
    {
        var expected = OperatingSystem.IsWindows();
        Assert.Equal(expected, LibraryPaths.IsSameOrUnder("/Books/a.epub", "/books"));
    }

    [Fact]
    public void Normalize_MakesFullPathWithoutTrailingSeparator() =>
        Assert.Equal(Path.GetFullPath("/books/fantasy"), LibraryPaths.Normalize("/books/./fantasy/"));
}
