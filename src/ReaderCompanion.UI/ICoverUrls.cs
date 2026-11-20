namespace ReaderCompanion.UI;

/// <summary>Turns a cover file name from the library into an image URL the window can load.</summary>
public interface ICoverUrls
{
    string UrlFor(string coverFileName);
}
