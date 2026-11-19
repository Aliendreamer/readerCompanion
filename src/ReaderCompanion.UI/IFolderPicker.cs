namespace ReaderCompanion.UI;

/// <summary>Opens the system folder picker. Returns null when the user cancels.</summary>
public interface IFolderPicker
{
    Task<string?> PickFolderAsync();
}
