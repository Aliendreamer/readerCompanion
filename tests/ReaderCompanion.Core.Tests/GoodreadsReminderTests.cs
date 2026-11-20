using ReaderCompanion.Core.Goodreads;

namespace ReaderCompanion.Core.Tests;

public class GoodreadsReminderTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(4, 3, true)]
    [InlineData(2, 3, false)]
    [InlineData(3, 3, true)]
    public void IsDue_AfterTheInterval(int monthsAgo, int interval, bool due)
    {
        Assert.Equal(due, GoodreadsReminder.IsDue(Now.AddMonths(-monthsAgo), interval, Now));
    }

    [Fact]
    public void NeverImported_IsNotDue()
    {
        Assert.False(GoodreadsReminder.IsDue(null, 3, Now));
    }
}
