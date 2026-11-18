namespace ReaderCompanion.Data;

public static class AppPaths
{
    public static string DatabasePath(string localAppDataRoot)
    {
        if (string.IsNullOrWhiteSpace(localAppDataRoot))
        {
            throw new InvalidOperationException(
                "Could not determine the local application data folder for this user, so the library database has no home. " +
                "On Linux, make sure HOME (or XDG_DATA_HOME) is set.");
        }

        return Path.Combine(localAppDataRoot, "ReaderCompanion", "library.db");
    }

    public static string DefaultDatabasePath() =>
        DatabasePath(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify));
}
