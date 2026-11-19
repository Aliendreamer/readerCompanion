namespace ReaderCompanion.Core.Library;

public enum CollectionKind
{
    Series,
    User,
}

public sealed class Collection
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string NormalizedName { get; set; } = "";

    public CollectionKind Kind { get; set; }
}
