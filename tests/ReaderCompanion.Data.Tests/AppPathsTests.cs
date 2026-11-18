namespace ReaderCompanion.Data.Tests;

public class AppPathsTests
{
    [Fact]
    public void DatabasePath_IsUnderReaderCompanionFolder() =>
        Assert.Equal(Path.Combine("/root", "ReaderCompanion", "library.db"), AppPaths.DatabasePath("/root"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DatabasePath_EmptyRoot_Throws(string root) =>
        Assert.Throws<InvalidOperationException>(() => AppPaths.DatabasePath(root));

    [Fact]
    public void DefaultDatabasePath_DataFolderNotCreatedYet_StillResolves()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "XDG_DATA_HOME drives LocalApplicationData on Linux only.");
        var missing = Path.Combine(Path.GetTempPath(), "rc-missing-" + Guid.NewGuid().ToString("N"));
        var previous = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", missing);
        try
        {
            Assert.Equal(Path.Combine(missing, "ReaderCompanion", "library.db"), AppPaths.DefaultDatabasePath());
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previous);
        }
    }
}
