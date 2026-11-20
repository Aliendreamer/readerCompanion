using Photino.NET;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Native file dialog on the app's main window.</summary>
internal sealed class PhotinoFilePicker(PhotinoWindow window) : IFilePicker
{
    public async Task<string?> PickFileAsync(string title, params string[] extensions)
    {
        (string Name, string[] Extensions)[] filters = extensions.Length == 0 ? [] : [("Files", [.. extensions.Select(e => "*." + e)])];
        var chosen = await window.ShowOpenFileAsync(title, null, false, filters);
        return chosen?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
    }
}
