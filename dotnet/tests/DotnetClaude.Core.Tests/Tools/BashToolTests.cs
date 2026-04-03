using System.Text.Json;
using DotnetClaude.Tools;
using DotnetClaude.Tools.BuiltIn;
using Xunit;

namespace DotnetClaude.Core.Tests.Tools;

public class BashToolTests
{
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    [Fact]
    public async Task ExecuteAsync_EchoHello_ReturnsSuccess()
    {
        // Arrange
        var tool = new BashTool();
        var input = new BashTool.Input("echo hello");
        var context = new ToolExecutionContext(
            "Bash",
            JsonSerializer.Serialize(input),
            _sink
        );

        // Act
        var result = await tool.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);
        Assert.Contains("hello", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCommand_ReturnsError()
    {
        // Arrange
        var tool = new BashTool();
        var input = new BashTool.Input("nonexistentcommand12345");
        var context = new ToolExecutionContext(
            "Bash",
            JsonSerializer.Serialize(input),
            _sink
        );

        // Act
        var result = await tool.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);
        Assert.True(result.Content.Contains("not found") || result.Content.Contains("is not recognized"));
    }

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent progressEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
