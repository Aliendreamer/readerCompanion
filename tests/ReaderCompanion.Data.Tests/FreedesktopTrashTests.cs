namespace ReaderCompanion.Data.Tests;

public sealed class FreedesktopTrashTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("rc-trash-").FullName;
    private readonly string _trash;
    private readonly FreedesktopTrash _sut;
    private static readonly DateTime When = new(2026, 11, 20, 14, 5, 9);

    public FreedesktopTrashTests()
    {
        _trash = Path.Combine(_root, "Trash");
        _sut = new FreedesktopTrash(_trash, () => When);
    }

    private string Write(string name)
    {
        var path = Path.Combine(_root, "my books", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, name);
        return path;
    }

    [Fact]
    public void MoveToTrash_MovesTheFile_AndWritesItsTrashInfo()
    {
        var path = Write("mort ü.pdf"); // under "<root>/my books/"

        Assert.True(_sut.MoveToTrash(path));

        Assert.False(File.Exists(path));
        Assert.Equal("mort ü.pdf", File.ReadAllText(Path.Combine(_trash, "files", "mort ü.pdf")));
        var info = File.ReadAllLines(Path.Combine(_trash, "info", "mort ü.pdf.trashinfo"));
        Assert.Equal("[Trash Info]", info[0]);
        Assert.StartsWith("Path=/", info[1], StringComparison.Ordinal);
        Assert.EndsWith("/my%20books/mort%20%C3%BC.pdf", info[1], StringComparison.Ordinal);
        Assert.Equal("DeletionDate=2026-11-20T14:05:09", info[2]);
    }

    [Fact]
    public void SameNameTwice_GetsAFreeName()
    {
        _sut.MoveToTrash(Write("a.epub"));

        Assert.True(_sut.MoveToTrash(Write("a.epub")));

        Assert.True(File.Exists(Path.Combine(_trash, "files", "a.2.epub")));
        Assert.True(File.Exists(Path.Combine(_trash, "info", "a.2.epub.trashinfo")));
    }

    [Fact]
    public void MissingFile_GivesFalse()
    {
        Assert.False(_sut.MoveToTrash(Path.Combine(_root, "nothing.epub")));
        Assert.False(Directory.Exists(Path.Combine(_trash, "info")) && Directory.EnumerateFiles(Path.Combine(_trash, "info")).Any());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
