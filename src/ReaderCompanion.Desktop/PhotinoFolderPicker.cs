using Photino.NET;
using ReaderCompanion.UI;

namespace ReaderCompanion.Desktop;

/// <summary>Native folder dialog on the app's main window.</summary>
internal sealed class PhotinoFolderPicker(PhotinoWindow window) : IFolderPicker
{
    public async Task<string?> PickFolderAsync()
    {
        var chosen = await window.ShowOpenFolderAsync("Link a folder of e-books");
        return chosen?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
    }
}
