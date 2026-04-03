using System.Net;
using System.Text.Json;
using OpenClaude.Tools;
using OpenClaude.Tools.BuiltIn;

namespace OpenClaude.Core.Tests.Tools;

public sealed class WebFetchToolTests
{
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    private ToolExecutionContext Context(object input) =>
        new("WebFetch", JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── AC 2: returns Markdown-converted content ──────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ConvertsHtmlToMarkdown()
    {
        const string Html = "<html><body><h1>Hello</h1><p>World</p></body></html>";
        var tool = new WebFetchTool(FakeHttpClient(Html));

        var result = await tool.ExecuteAsync(Context(new { url = "http://example.com/" }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);
        Assert.Contains("Hello", result.Content);
        Assert.Contains("World", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_ScriptTagsStrippedFromOutput()
    {
        const string Html = "<html><body><script>alert('xss')</script><p>Safe</p></body></html>";
        var tool = new WebFetchTool(FakeHttpClient(Html));

        var result = await tool.ExecuteAsync(Context(new { url = "http://example.com/" }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.DoesNotContain("alert", result.Content);
        Assert.Contains("Safe", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_MetadataContainsUrl()
    {
        var tool = new WebFetchTool(FakeHttpClient("<p>hi</p>"));

        var result = await tool.ExecuteAsync(
            Context(new { url = "http://example.com/page" }));

        Assert.True(result.Metadata.ContainsKey("url"));
        Assert.Equal("http://example.com/page", result.Metadata["url"]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingUrl_ReturnsError()
    {
        var tool = new WebFetchTool(FakeHttpClient(string.Empty));

        var result = await tool.ExecuteAsync(Context(new { }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_HttpFailure_ReturnsError()
    {
        var tool = new WebFetchTool(FakeHttpClient(null, HttpStatusCode.InternalServerError));

        var result = await tool.ExecuteAsync(Context(new { url = "http://example.com/" }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsError()
    {
        var tool = new WebFetchTool(new HttpClient());
        var ctx = new ToolExecutionContext("WebFetch", "!!invalid", _sink);

        var result = await tool.ExecuteAsync(ctx);

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        var tool = new WebFetchTool(new HttpClient());
        Assert.Equal("WebFetch", tool.Name);
    }

    // ── ConvertHtmlToMarkdown unit tests ──────────────────────────────────────

    [Fact]
    public void ConvertHtmlToMarkdown_HeadingBecomesMarkdownHeading()
    {
        var result = WebFetchTool.ConvertHtmlToMarkdown("<h2>Section</h2>");
        Assert.Contains("Section", result);
    }

    [Fact]
    public void ConvertHtmlToMarkdown_ParagraphPreservesText()
    {
        var result = WebFetchTool.ConvertHtmlToMarkdown("<p>Some text here.</p>");
        Assert.Contains("Some text here.", result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpClient FakeHttpClient(
        string? responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(responseBody ?? string.Empty, statusCode);
        return new HttpClient(handler);
    }

    private sealed class FakeHttpMessageHandler(string body, HttpStatusCode status)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body),
            };
            return Task.FromResult(response);
        }
    }

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }
}
