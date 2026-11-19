namespace ReaderCompanion.Core.Metadata;

/// <summary>Details read from an e-book file. Missing details are null or empty.</summary>
public sealed record BookMetadata
{
    public string? Title { get; init; }

    public IReadOnlyList<string> Authors { get; init; } = [];

    public string? Language { get; init; }

    public string? Isbn { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];

    public string? Series { get; init; }

    public decimal? SeriesIndex { get; init; }

    public CoverImage? Cover { get; init; }
}

/// <summary>Cover image bytes and the file extension to store them with (for example ".jpg").</summary>
public sealed record CoverImage(byte[] Bytes, string Extension);
