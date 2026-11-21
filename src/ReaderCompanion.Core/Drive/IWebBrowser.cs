namespace ReaderCompanion.Core.Drive;

/// <summary>Opens web pages in the user's browser.</summary>
public interface IWebBrowser
{
    bool Open(Uri url);
}
