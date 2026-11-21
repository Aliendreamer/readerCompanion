namespace ReaderCompanion.Core.Organising;

/// <summary>One organise that was applied: when, where and the moves it made, so it can be undone.</summary>
public sealed class FileOperation
{
    public int Id { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string Root { get; set; } = "";

    public DateTime? UndoneUtc { get; set; }

    public List<FileMove> Moves { get; set; } = [];
}

public sealed class FileMove
{
    public int Id { get; set; }

    public int OperationId { get; set; }

    public int BookFileId { get; set; }

    public string From { get; set; } = "";

    public string To { get; set; } = "";

    /// <summary>True once the file was actually moved.</summary>
    public bool Done { get; set; }
}

public sealed record MoveFailure(string From, string Reason);

public sealed record OrganiseResult(int Moved, IReadOnlyList<MoveFailure> Failed);

public sealed record LastOrganise(DateTime CreatedUtc, string Root, int FileCount);

/// <summary>What an organise is doing.</summary>
public enum OrganiseStage
{
    /// <summary>Working out the moves (on Drive, listing the folder tree first).</summary>
    Planning,
    Moving,
    MovingBack,

    /// <summary>Removing folders the moves left empty.</summary>
    Tidying,
}

/// <summary>How far an organise has got: <see cref="Done"/> of <see cref="Total"/> files while moving.</summary>
public sealed record OrganiseProgress(OrganiseStage Stage, int Done = 0, int Total = 0);

/// <summary>Moves a library folder's files into a layout, and back. Never overwrites a file; the library follows
/// every move. Apply and undo refuse while a scan runs (and scans wait for them).</summary>
public interface IOrganiser
{
    /// <summary>The moves that would put the present files under <paramref name="root"/> into the layout.</summary>
    Task<IReadOnlyList<Move>> PreviewAsync(
        string root, OrganiseLayout layout, IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Moves the files, records the operation and removes folders left empty under the root.</summary>
    Task<OrganiseResult> ApplyAsync(
        string root, IReadOnlyList<Move> moves, IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>The latest organise that can be undone, or null.</summary>
    Task<LastOrganise?> LastAsync(CancellationToken cancellationToken = default);

    /// <summary>Moves the files of the latest organise back, skipping any moved or replaced since.</summary>
    Task<OrganiseResult> UndoLastAsync(IProgress<OrganiseProgress>? progress = null, CancellationToken cancellationToken = default);
}
