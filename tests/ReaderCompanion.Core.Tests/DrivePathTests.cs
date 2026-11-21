using ReaderCompanion.Core.Drive;

namespace ReaderCompanion.Core.Tests;

public class DrivePathTests
{
    [Fact]
    public void Make_KeepsTheIdAndTheDisplayPath()
    {
        var path = DrivePath.Make("1AbC", "Books/Fantasy/The Hobbit.epub");

        Assert.Equal("gdrive://1AbC/Books/Fantasy/The Hobbit.epub", path);
        Assert.True(DrivePath.IsDrive(path));
        Assert.Equal("Books/Fantasy/The Hobbit.epub", DrivePath.Display(path));
    }

    [Theory]
    [InlineData("/home/me/books/a.epub")]
    [InlineData(@"C:\books\a.epub")]
    public void LocalPaths_AreNotDrive(string path)
    {
        Assert.False(DrivePath.IsDrive(path));
        Assert.Equal(path, DrivePath.Display(path));
    }

    [Fact]
    public void Under_MatchesTheFolderAndItsSubfolders_NotLookalikes()
    {
        Assert.True(DrivePath.IsUnder(DrivePath.Make("1", "Books/a.epub"), "Books"));
        Assert.True(DrivePath.IsUnder(DrivePath.Make("2", "Books/Sub/b.epub"), "Books"));
        Assert.False(DrivePath.IsUnder(DrivePath.Make("3", "Books2/c.epub"), "Books"));
        Assert.False(DrivePath.IsUnder("/Books/a.epub", "Books"));
    }
}
