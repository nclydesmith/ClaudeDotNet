using System.Net.Http;
using System.Text.Json;
using DotnetClaude.Tools;
using DotnetClaude.Tools.BuiltIn;
using Xunit;

namespace DotnetClaude.Core.Tests.Tools;

public sealed class WebFetchToolTests
{
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    private ToolExecutionContext Context(object input) =>
        new("WebFetch", JsonSerializer.Serialize(input), _sink);

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess()
    {
        var tool = new WebFetchTool(new HttpClient());
        var result = await tool.ExecuteAsync(Context(new { url = "https://example.com" }), CancellationToken.None);
        Assert.Equal(ToolResultStatus.Success, result.Status);
    }

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) => ValueTask.CompletedTask;
    }
}
