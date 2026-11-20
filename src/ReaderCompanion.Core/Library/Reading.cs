namespace ReaderCompanion.Core.Library;

/// <summary>Where the user is with a book.</summary>
public enum ReadStatus
{
    NotStarted,
    WantToRead,
    Reading,
    Read,
    Abandoned,
}

/// <summary>One read of a book. Open while <see cref="FinishedOn"/> is null; an abandoned read is not a read.</summary>
public sealed class ReadingSession
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public DateOnly? StartedOn { get; set; }

    public DateOnly? FinishedOn { get; set; }

    public bool Abandoned { get; set; }
}

/// <summary>Keeps a book's reading sessions in step with the status the user sets.</summary>
public static class ReadingLog
{
    /// <summary>Reading opens a session (unless one is open); read finishes the open one, or records a read
    /// finished today; abandoned closes the open one as abandoned; other statuses change nothing.</summary>
    public static void ChangeStatus(ICollection<ReadingSession> sessions, ReadStatus status, DateOnly today)
    {
        var open = sessions.FirstOrDefault(s => s.FinishedOn is null);
        switch (status)
        {
            case ReadStatus.Reading when open is null:
                sessions.Add(new ReadingSession { StartedOn = today });
                break;
            case ReadStatus.Read when open is null:
                sessions.Add(new ReadingSession { FinishedOn = today });
                break;
            case ReadStatus.Read:
                open.FinishedOn = today;
                break;
            case ReadStatus.Abandoned when open is not null:
                open.FinishedOn = today;
                open.Abandoned = true;
                break;
        }
    }

    /// <summary>Why a read with these dates cannot be saved, or null when it can.</summary>
    public static string? DateError(DateOnly? startedOn, DateOnly? finishedOn) =>
        startedOn is { } start && finishedOn is { } finish && finish < start ? "A read cannot finish before it starts." : null;

    /// <summary>After a merge a book can hold several open reads; they are one read. Keeps the first open session,
    /// started at the earliest start, and returns the others to remove.</summary>
    public static IReadOnlyList<ReadingSession> CombineOpen(IEnumerable<ReadingSession> sessions)
    {
        var open = sessions.Where(s => s.FinishedOn is null).ToList();
        if (open.Count < 2)
        {
            return [];
        }

        open[0].StartedOn = open.Select(s => s.StartedOn).Where(d => d is not null).Min();
        return open[1..];
    }

    /// <summary>How many times the book was read to the end.</summary>
    public static int Reads(IEnumerable<ReadingSession> sessions) => sessions.Count(s => s.FinishedOn is not null && !s.Abandoned);
}
