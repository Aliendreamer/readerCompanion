using System.Text.Json;
using System.Text.Json.Serialization;
using ReaderCompanion.Core.Preferences;

namespace ReaderCompanion.Data;

/// <summary>Preferences kept in <c>preferences.json</c> next to the database.</summary>
internal sealed class JsonAppPreferences : IAppPreferences
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public JsonAppPreferences(LibraryDataPaths paths)
    {
        _path = Path.Combine(paths.DataFolder, "preferences.json");
        Current = Load(_path);
    }

    public AppPreferences Current { get; private set; }

    public event Action? Changed;

    public void Save(AppPreferences preferences)
    {
        Current = preferences;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(preferences, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Not remembered after a restart, but still in effect now.
        }

        Changed?.Invoke();
    }

    private static AppPreferences Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path), Options) ?? new AppPreferences()
                : new AppPreferences();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppPreferences();
        }
    }
}
