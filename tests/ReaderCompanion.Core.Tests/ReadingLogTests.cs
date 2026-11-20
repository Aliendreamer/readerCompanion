using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Core.Tests;

public class ReadingLogTests
{
    private static readonly DateOnly Today = new(2026, 3, 5);

    private static ReadingSession Session(DateOnly? started, DateOnly? finished, bool abandoned = false) =>
        new() { StartedOn = started, FinishedOn = finished, Abandoned = abandoned };

    [Fact]
    public void Reading_OpensASession_UnlessOneIsOpen()
    {
        List<ReadingSession> sessions = [];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Reading, Today);
        ReadingLog.ChangeStatus(sessions, ReadStatus.Reading, Today.AddDays(1));

        var open = Assert.Single(sessions);
        Assert.Equal(Today, open.StartedOn);
        Assert.Null(open.FinishedOn);
    }

    [Fact]
    public void Read_FinishesTheOpenSession()
    {
        List<ReadingSession> sessions = [Session(new(2026, 3, 1), null)];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Read, Today);

        Assert.Equal(Today, Assert.Single(sessions).FinishedOn);
        Assert.Equal(1, ReadingLog.Reads(sessions));
    }

    [Fact]
    public void Read_WithoutAnOpenSession_RecordsAFinishedRead()
    {
        List<ReadingSession> sessions = [Session(new(2024, 1, 1), new(2024, 2, 1))];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Read, Today);

        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.StartedOn is null && s.FinishedOn == Today);
        Assert.Equal(2, ReadingLog.Reads(sessions));
    }

    [Fact]
    public void ReadAgain_CountsTwice()
    {
        List<ReadingSession> sessions = [Session(new(2024, 1, 1), new(2024, 2, 1))];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Reading, new(2026, 3, 3));
        ReadingLog.ChangeStatus(sessions, ReadStatus.Read, new(2026, 3, 20));

        Assert.Equal(2, ReadingLog.Reads(sessions));
    }

    [Fact]
    public void Abandoned_ClosesTheOpenSessionAsAbandoned_WhichIsNotARead()
    {
        List<ReadingSession> sessions = [Session(new(2026, 3, 1), null)];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Abandoned, Today);

        var session = Assert.Single(sessions);
        Assert.Equal(Today, session.FinishedOn);
        Assert.True(session.Abandoned);
        Assert.Equal(0, ReadingLog.Reads(sessions));
    }

    [Theory]
    [InlineData(ReadStatus.NotStarted)]
    [InlineData(ReadStatus.WantToRead)]
    public void OtherStatuses_LeaveSessionsAlone(ReadStatus status)
    {
        List<ReadingSession> sessions = [Session(new(2026, 3, 1), null)];

        ReadingLog.ChangeStatus(sessions, status, Today);

        Assert.Null(Assert.Single(sessions).FinishedOn);
    }

    [Fact]
    public void CombineOpen_KeepsOneOpenRead_FromTheEarliestStart()
    {
        var later = Session(new(2026, 3, 1), null);
        var earlier = Session(new(2026, 2, 1), null);
        var done = Session(new(2025, 1, 1), new(2025, 2, 1));

        var removed = ReadingLog.CombineOpen([later, done, earlier]);

        Assert.Equal([earlier], removed);
        Assert.Equal(new DateOnly(2026, 2, 1), later.StartedOn);
    }

    [Theory]
    [InlineData("2026-02-12", "2026-02-01", true)]
    [InlineData("2026-02-01", "2026-02-12", false)]
    [InlineData(null, "2026-02-01", false)]
    public void DateError_RefusesAFinishBeforeTheStart(string? started, string? finished, bool refused)
    {
        var error = ReadingLog.DateError(
            started is null ? null : DateOnly.Parse(started, System.Globalization.CultureInfo.InvariantCulture),
            finished is null ? null : DateOnly.Parse(finished, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(refused, error is not null);
    }

    [Fact]
    public void Abandoned_WithoutAnOpenSession_ChangesNothing()
    {
        List<ReadingSession> sessions = [];

        ReadingLog.ChangeStatus(sessions, ReadStatus.Abandoned, Today);

        Assert.Empty(sessions);
    }
}
