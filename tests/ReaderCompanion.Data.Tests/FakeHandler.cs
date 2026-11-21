namespace ReaderCompanion.Data.Tests;

/// <summary>Answers HTTP requests without a network.</summary>
internal sealed class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
{
    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer)
        : this(request => Task.FromResult(answer(request)))
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken); // so the answer can read the body
        }

        return await answer(request);
    }
}
