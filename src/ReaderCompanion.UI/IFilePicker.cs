namespace ReaderCompanion.UI;

/// <summary>Opens the system file picker. Returns null when the user cancels.</summary>
public interface IFilePicker
{
    /// <param name="extensions">Allowed extensions without the dot, e.g. "csv".</param>
    Task<string?> PickFileAsync(string title, params string[] extensions);
}
