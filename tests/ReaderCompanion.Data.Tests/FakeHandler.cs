namespace ReaderCompanion.Data.Tests;

/// <summary>Answers HTTP requests without a network.</summary>
internal sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken); // so the answer can read the body
        }

        return answer(request);
    }
}
