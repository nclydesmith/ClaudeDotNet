using System.Text.Json;
using DotnetClaude.Tools;
using DotnetClaude.Tools.BuiltIn;
using Xunit;

namespace DotnetClaude.Core.Tests.Tools;

public sealed class AgentToolTests
{
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    private ToolExecutionContext Context(object input) =>
        new("Agent", JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── AC 1: ExecuteAsync creates child runner, returns final text ───────────

    [Fact]
    public async Task ExecuteAsync_ReturnsChildRunnerResponse()
    {
        const string ExpectedText = "The answer is 42.";
        var tool = new AgentTool(new StubChildRunner(ExpectedText));

        var result = await tool.ExecuteAsync(Context(new { prompt = "What is the answer?" }), CancellationToken.None);

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal(ExpectedText, result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_PassesPromptToChildRunner()
    {
        const string Prompt = "Summarise the report.";
        string? capturedPrompt = null;
        var runner = new CapturingChildRunner(result: "Done.", onRun: p => capturedPrompt = p);
        var tool = new AgentTool(runner);

        await tool.ExecuteAsync(Context(new { prompt = Prompt }), CancellationToken.None);

        Assert.Equal(Prompt, capturedPrompt);
    }

    [Fact]
    public async Task ExecuteAsync_MissingPrompt_ReturnsError()
    {
        var tool = new AgentTool(new StubChildRunner("unused"));

        var result = await tool.ExecuteAsync(Context(new { }), CancellationToken.None);

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ChildRunnerThrows_ReturnsError()
    {
        var tool = new AgentTool(new ThrowingChildRunner());

        var result = await tool.ExecuteAsync(Context(new { prompt = "Hello" }), CancellationToken.None);

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.Contains("Agent execution failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ReturnsError()
    {
        var tool = new AgentTool(new StubChildRunner("unused"));
        var ctx = new ToolExecutionContext("Agent", "not-valid-json", _sink);

        var result = await tool.ExecuteAsync(ctx, CancellationToken.None);

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        var tool = new AgentTool(new StubChildRunner("x"));
        Assert.Equal("Agent", tool.Name);
    }

    // ── AC 4: integration – all three tools can be registered and resolved ────

    [Fact]
    public void AllThreeTools_CanBeRegisteredAndResolvedByName()
    {
        var registry = new ToolRegistry();

        registry.Register(new AgentTool(new StubChildRunner("x")));
        registry.Register(new WebFetchTool(new HttpClient()));
        registry.Register(new WebSearchTool(new StubSearchProvider([])));

        Assert.NotNull(registry.Resolve("Agent"));
        Assert.NotNull(registry.Resolve("WebFetch"));
        Assert.NotNull(registry.Resolve("WebSearch"));
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class StubChildRunner(string result) : IChildAgentRunner
    {
        public Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class CapturingChildRunner(string result, Action<string> onRun) : IChildAgentRunner
    {
        public Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default)
        {
            onRun(prompt);
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingChildRunner : IChildAgentRunner
    {
        public Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated child engine failure.");
    }

    private sealed class StubSearchProvider(IReadOnlyList<WebSearchResult> results) : IWebSearchProvider
    {
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
            string query, CancellationToken cancellationToken = default)
            => Task.FromResult(results);
    }

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }
}
