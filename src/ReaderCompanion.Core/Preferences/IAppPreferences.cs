namespace ReaderCompanion.Core.Preferences;

/// <summary>Which theme the app uses: follow the system, or the user's own pick.</summary>
public enum ThemeChoice
{
    System,
    Light,
    Dark,
}

/// <summary>How the Library page shows books.</summary>
public enum LibraryView
{
    Grid,
    Table,
}

/// <summary>How the Library page groups the books it shows.</summary>
public enum LibraryGroup
{
    None,
    Author,
    Collection,
    Genre,
}

/// <summary>How the Library page orders books.</summary>
public enum LibrarySort
{
    Title,
    Author,
    Series,
    DateAdded,
}

public sealed record AppPreferences(
    ThemeChoice Theme = ThemeChoice.System,
    LibraryView LibraryView = LibraryView.Grid,
    LibraryGroup LibraryGroup = LibraryGroup.None,
    LibrarySort LibrarySort = LibrarySort.Title);

/// <summary>The user's remembered choices. Reads never fail: a missing or damaged store gives the defaults.</summary>
public interface IAppPreferences
{
    AppPreferences Current { get; }

    /// <summary>Raised after <see cref="Save"/> changes <see cref="Current"/>.</summary>
    event Action? Changed;

    /// <summary>Makes <paramref name="preferences"/> current and stores it; if storing fails it still applies for
    /// this session.</summary>
    void Save(AppPreferences preferences);
}
