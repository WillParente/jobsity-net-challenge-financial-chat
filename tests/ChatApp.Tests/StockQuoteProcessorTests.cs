using System.Net;
using ChatApp.StockBot;
using ChatApp.StockBot.Stooq;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatApp.Tests;

/// <summary>
/// Exercises the bot pipeline (HTTP -> CSV parse -> format) against a
/// stubbed HttpMessageHandler — no network involved. Covers the bonus
/// requirement that exceptions inside the bot never escape: every failure
/// becomes a friendly chat reply.
/// </summary>
public class StockQuoteProcessorTests
{
    private const string ValidCsv =
        "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
        "AAPL.US,2026-07-30,22:00:07,208.9,213.58,208.1,213.42,49500000\n";

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _responder;
        public int CallCount { get; private set; }

        public StubHandler(Func<int, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responder(CallCount));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(_exception);
    }

    private static StockQuoteProcessor CreateProcessor(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://stub.local/") };
        var client = new StooqClient(httpClient);
        return new StockQuoteProcessor(client, NullLogger<StockQuoteProcessor>.Instance);
    }

    private static HttpResponseMessage CsvResponse(string csv) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(csv),
    };

    [Fact]
    public async Task BuildReply_ValidTicker_ReturnsFormattedQuote()
    {
        var processor = CreateProcessor(new StubHandler(_ => CsvResponse(ValidCsv)));

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Equal("AAPL.US quote is $213.42 per share", reply);
    }

    [Fact]
    public async Task BuildReply_UnknownTicker_ReturnsNotFoundMessage()
    {
        const string notFoundCsv =
            "Symbol,Date,Time,Open,High,Low,Close,Volume\n" +
            "FAKE123,N/D,N/D,N/D,N/D,N/D,N/D,N/D\n";
        var processor = CreateProcessor(new StubHandler(_ => CsvResponse(notFoundCsv)));

        var reply = await processor.BuildReplyAsync("fake123", CancellationToken.None);

        Assert.Contains("No quote is available", reply);
        Assert.Contains("FAKE123", reply);
    }

    [Fact]
    public async Task BuildReply_HttpErrorOnEveryAttempt_ReturnsServiceUnavailableMessage()
    {
        var processor = CreateProcessor(
            new ThrowingHandler(new HttpRequestException("connection refused")));

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Contains("not responding", reply);
    }

    [Fact]
    public async Task BuildReply_Timeout_ReturnsServiceUnavailableMessage()
    {
        var processor = CreateProcessor(
            new ThrowingHandler(new TaskCanceledException("request timed out")));

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Contains("not responding", reply);
    }

    [Fact]
    public async Task BuildReply_ServerError_ReturnsServiceUnavailableMessage()
    {
        var processor = CreateProcessor(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Contains("not responding", reply);
    }

    [Fact]
    public async Task BuildReply_TransientFailure_IsRetriedOnce()
    {
        var handler = new StubHandler(call => call == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : CsvResponse(ValidCsv));
        var processor = CreateProcessor(handler);

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Equal("AAPL.US quote is $213.42 per share", reply);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task BuildReply_MalformedPayload_ReturnsFriendlyMessage()
    {
        var processor = CreateProcessor(new StubHandler(_ => CsvResponse("<html>not csv</html>")));

        var reply = await processor.BuildReplyAsync("aapl.us", CancellationToken.None);

        Assert.Contains("Could not read", reply);
    }
}
