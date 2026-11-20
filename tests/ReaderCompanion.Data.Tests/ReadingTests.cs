using Microsoft.Extensions.DependencyInjection;
using ReaderCompanion.Core.Library;

namespace ReaderCompanion.Data.Tests;

public sealed class ReadingTests : IDisposable
{
    private readonly TempLibrary _library = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly DateOnly Today = new(2026, 3, 5);

    private IServiceProvider Scope => _library.Services.CreateScope().ServiceProvider;

    private IReading Reading => Scope.GetRequiredService<IReading>();

    private int Add(string title, bool withFile = true)
    {
        using var db = _library.NewContext();
        var book = new Book { Title = title };
        if (withFile)
        {
            book.Files.Add(TestFiles.File($"/b/{title}.epub"));
        }

        db.Books.Add(book);
        db.SaveChanges();
        return book.Id;
    }

    [Fact]
    public async Task NewBook_IsNotStarted_WithNothingRecorded()
    {
        var info = await Reading.GetAsync(Add("Mort"), Token);

        Assert.NotNull(info);
        Assert.Equal(ReadStatus.NotStarted, info.Status);
        Assert.Null(info.Rating);
        Assert.Null(info.Notes);
        Assert.Empty(info.Sessions);
    }

    [Fact]
    public async Task SettingStatus_RecordsSessions_AndTheListShowsStatus()
    {
        var mort = Add("Mort");

        await Reading.SetStatusAsync(mort, ReadStatus.Reading, Today, Token);
        await Reading.SetStatusAsync(mort, ReadStatus.Read, Today.AddDays(10), Token);

        var info = (await Reading.GetAsync(mort, Token))!;
        Assert.Equal(ReadStatus.Read, info.Status);
        var session = Assert.Single(info.Sessions);
        Assert.Equal((Today, Today.AddDays(10)), (session.StartedOn, session.FinishedOn));
        var item = Assert.Single(await Scope.GetRequiredService<ILibraryBooks>().ListAsync(Token));
        Assert.Equal(ReadStatus.Read, item.ReadStatus);
        Assert.True(item.Owned);
    }

    [Fact]
    public async Task RatingAndNotes_SetAndClear()
    {
        var mort = Add("Mort");

        await Reading.SetRatingAsync(mort, 4, Token);
        await Reading.SetNotesAsync(mort, "  Death takes an apprentice.  ", Token);
        var info = (await Reading.GetAsync(mort, Token))!;
        Assert.Equal(4, info.Rating);
        Assert.Equal("Death takes an apprentice.", info.Notes);
        Assert.Equal(4, Assert.Single(await Scope.GetRequiredService<ILibraryBooks>().ListAsync(Token)).Rating);

        await Reading.SetRatingAsync(mort, null, Token);
        await Reading.SetNotesAsync(mort, "  ", Token);
        info = (await Reading.GetAsync(mort, Token))!;
        Assert.Null(info.Rating);
        Assert.Null(info.Notes);
    }

    [Fact]
    public async Task RatingOutsideOneToFive_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Reading.SetRatingAsync(Add("Mort"), 6, Token));
    }

    [Fact]
    public async Task Sessions_CanBeCorrected_AndDeleted_NewestFirst()
    {
        var mort = Add("Mort");
        await Reading.SetStatusAsync(mort, ReadStatus.Read, new DateOnly(2024, 5, 1), Token);
        await Reading.SetStatusAsync(mort, ReadStatus.Reading, Today, Token);
        await Reading.SetStatusAsync(mort, ReadStatus.Read, Today, Token);
        var sessions = (await Reading.GetAsync(mort, Token))!.Sessions;
        Assert.Equal([Today, new DateOnly(2024, 5, 1)], sessions.Select(s => s.FinishedOn!.Value));

        await Reading.UpdateSessionAsync(sessions[0].Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 12), Token);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Reading.UpdateSessionAsync(sessions[0].Id, new DateOnly(2026, 2, 12), new DateOnly(2026, 2, 1), Token));
        await Reading.DeleteSessionAsync(sessions[1].Id, Token);

        var left = Assert.Single((await Reading.GetAsync(mort, Token))!.Sessions);
        Assert.Equal((new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 12)), (left.StartedOn, left.FinishedOn));
    }

    [Fact]
    public async Task UnknownBook_GivesNullOrFalse()
    {
        Assert.Null(await Reading.GetAsync(999, Token));
        Assert.False(await Reading.SetStatusAsync(999, ReadStatus.Read, Today, Token));
    }

    [Fact]
    public async Task BookWithoutFiles_IsNotOwned()
    {
        Add("Wanted", withFile: false);

        Assert.False(Assert.Single(await Scope.GetRequiredService<ILibraryBooks>().ListAsync(Token)).Owned);
    }

    [Fact]
    public async Task Merge_CombinesHistories_AndFillsMissingRatingAndNotes()
    {
        var keep = Add("Mort");
        var other = Add("Mort copy");
        await Reading.SetStatusAsync(keep, ReadStatus.Read, new DateOnly(2024, 1, 1), Token);
        await Reading.SetStatusAsync(other, ReadStatus.Read, new DateOnly(2025, 1, 1), Token);
        await Reading.SetRatingAsync(other, 5, Token);
        await Reading.SetNotesAsync(other, "Loved it", Token);

        await Scope.GetRequiredService<IDuplicates>().MergeAsync(keep, [other], Token);

        var info = (await Reading.GetAsync(keep, Token))!;
        Assert.Equal(2, ReadingLog.Reads(info.Sessions.Select(s => new ReadingSession { FinishedOn = s.FinishedOn, Abandoned = s.Abandoned })));
        Assert.Equal(ReadStatus.Read, info.Status);
        Assert.Equal(5, info.Rating);
        Assert.Equal("Loved it", info.Notes);
    }

    [Fact]
    public async Task Merge_TwoOpenReads_BecomeOne()
    {
        var keep = Add("Mort");
        var other = Add("Mort copy");
        await Reading.SetStatusAsync(keep, ReadStatus.Reading, new DateOnly(2026, 3, 1), Token);
        await Reading.SetStatusAsync(other, ReadStatus.Reading, new DateOnly(2026, 2, 1), Token);

        await Scope.GetRequiredService<IDuplicates>().MergeAsync(keep, [other], Token);

        var open = Assert.Single((await Reading.GetAsync(keep, Token))!.Sessions);
        Assert.Equal(new DateOnly(2026, 2, 1), open.StartedOn);
        Assert.Null(open.FinishedOn);
    }

    [Fact]
    public async Task SettingTheSameStatusAgain_ChangesNothing()
    {
        var mort = Add("Mort");
        await Reading.SetStatusAsync(mort, ReadStatus.Read, Today, Token);

        await Reading.SetStatusAsync(mort, ReadStatus.Read, Today, Token);

        Assert.Single((await Reading.GetAsync(mort, Token))!.Sessions);
    }

    [Fact]
    public async Task Merge_WithAnOpenRead_IsReading_AndKeepsBothNotes()
    {
        var keep = Add("Mort");
        var other = Add("Mort copy");
        await Reading.SetStatusAsync(keep, ReadStatus.WantToRead, Today, Token);
        await Reading.SetNotesAsync(keep, "Gift from Sam", Token);
        await Reading.SetStatusAsync(other, ReadStatus.Reading, Today, Token);
        await Reading.SetNotesAsync(other, "Up to chapter 3", Token);

        await Scope.GetRequiredService<IDuplicates>().MergeAsync(keep, [other], Token);

        var info = (await Reading.GetAsync(keep, Token))!;
        Assert.Equal(ReadStatus.Reading, info.Status);
        Assert.Equal("Gift from Sam\n\nUp to chapter 3", info.Notes);
    }

    public void Dispose() => _library.Dispose();
}
