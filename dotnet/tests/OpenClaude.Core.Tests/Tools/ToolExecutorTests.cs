using OpenClaude.Tools;

namespace OpenClaude.Core.Tests.Tools;

public sealed class ToolExecutorTests
{
    // ── Test doubles ──────────────────────────────────────────────────────────

    /// <summary>Always returns a fixed <see cref="ToolResult"/>.</summary>
    private sealed class ConstantTool(string name, ToolResult result) : ITool
    {
        public string Name { get; } = name;
        public string Description => "constant stub";

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    /// <summary>Tracks every call to <see cref="ExecuteAsync"/>.</summary>
    private sealed class SpyTool(string name) : ITool
    {
        public string Name { get; } = name;
        public string Description => "spy stub";
        public int CallCount { get; private set; }

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(ToolResult.Succeeded("spy result"));
        }
    }

    /// <summary>Permission policy that always denies.</summary>
    private sealed class DenyAllPolicy : ICanUseTool
    {
        public Task<bool> IsAllowedAsync(string toolName, string inputJson, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    /// <summary>Permission policy that always allows.</summary>
    private sealed class AllowAllPolicy : ICanUseTool
    {
        public Task<bool> IsAllowedAsync(string toolName, string inputJson, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    /// <summary>Collects all progress events emitted during execution.</summary>
    private sealed class CapturingProgressSink : IToolProgressSink
    {
        private readonly List<ToolProgressEvent> _events = [];
        public IReadOnlyList<ToolProgressEvent> Events => _events;

        public ValueTask ReportAsync(ToolProgressEvent progressEvent, CancellationToken cancellationToken = default)
        {
            _events.Add(progressEvent);
            return ValueTask.CompletedTask;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (ToolExecutor Executor, ToolRegistry Registry) BuildExecutor(ITool? tool = null)
    {
        var registry = new ToolRegistry();
        if (tool is not null)
            registry.Register(tool);
        var executor = new ToolExecutor(registry);
        return (executor, registry);
    }

    private static ToolExecutionContext MakeContext(string toolName, IToolProgressSink? sink = null) =>
        new(toolName, "{}", sink ?? new CapturingProgressSink());

    // ── Permission denied path ────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenPermissionDenied_DoesNotCallTool_And_ReturnsDeniedStatus()
    {
        var spy = new SpyTool("echo");
        var (executor, _) = BuildExecutor(spy);
        var context = MakeContext("echo");

        ToolResult result = await executor.ExecuteAsync(context, new DenyAllPolicy());

        Assert.Equal(ToolResultStatus.Denied, result.Status);
        Assert.Equal(0, spy.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionDenied_DoesNotEmitProgressEvents()
    {
        var spy = new SpyTool("echo");
        var (executor, _) = BuildExecutor(spy);
        var sink = new CapturingProgressSink();
        var context = MakeContext("echo", sink);

        await executor.ExecuteAsync(context, new DenyAllPolicy());

        Assert.Empty(sink.Events);
    }

    // ── Permission granted path ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenPermissionGranted_InvokesTool_And_ReturnsItsResult()
    {
        var tool = new ConstantTool("calc", ToolResult.Succeeded("42"));
        var (executor, _) = BuildExecutor(tool);
        var context = MakeContext("calc");

        ToolResult result = await executor.ExecuteAsync(context, new AllowAllPolicy());

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal("42", result.Output);
    }

    // ── Progress streaming ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmitsStartedEvent_ThenCompletedEvent_ToProgressSink()
    {
        var tool = new ConstantTool("printer", ToolResult.Succeeded("printed"));
        var (executor, _) = BuildExecutor(tool);
        var sink = new CapturingProgressSink();
        var context = MakeContext("printer", sink);

        await executor.ExecuteAsync(context, new AllowAllPolicy());

        Assert.Equal(2, sink.Events.Count);
        Assert.IsType<ToolStartedEvent>(sink.Events[0]);
        Assert.IsType<ToolCompletedEvent>(sink.Events[1]);
    }

    [Fact]
    public async Task ExecuteAsync_StartedEvent_ContainsToolNameAndInputJson()
    {
        var tool = new ConstantTool("read_file", ToolResult.Succeeded("content"));
        var (executor, _) = BuildExecutor(tool);
        var sink = new CapturingProgressSink();
        var context = new ToolExecutionContext("read_file", @"{""path"":""/tmp/x""}", sink);

        await executor.ExecuteAsync(context, new AllowAllPolicy());

        var started = Assert.IsType<ToolStartedEvent>(sink.Events[0]);
        Assert.Equal("read_file", started.ToolName);
        Assert.Equal(@"{""path"":""/tmp/x""}", started.InputJson);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedEvent_ContainsToolResult()
    {
        var tool = new ConstantTool("write_file", ToolResult.Succeeded("written"));
        var (executor, _) = BuildExecutor(tool);
        var sink = new CapturingProgressSink();
        var context = MakeContext("write_file", sink);

        ToolResult result = await executor.ExecuteAsync(context, new AllowAllPolicy());

        var completed = Assert.IsType<ToolCompletedEvent>(sink.Events[1]);
        Assert.Same(result, completed.Result);
    }

    // ── ToolPermissionPolicy composition ─────────────────────────────────────

    [Fact]
    public async Task ToolPermissionPolicy_NoChecks_AllowsAll()
    {
        var policy = new ToolPermissionPolicy();

        bool allowed = await policy.IsAllowedAsync("any", "{}");

        Assert.True(allowed);
    }

    [Fact]
    public async Task ToolPermissionPolicy_WithOneDenyCheck_Denies()
    {
        var policy = new ToolPermissionPolicy([new DenyAllPolicy()]);

        bool allowed = await policy.IsAllowedAsync("any", "{}");

        Assert.False(allowed);
    }

    [Fact]
    public async Task ToolPermissionPolicy_AllAllowChecks_Allows()
    {
        var policy = new ToolPermissionPolicy([new AllowAllPolicy(), new AllowAllPolicy()]);

        bool allowed = await policy.IsAllowedAsync("any", "{}");

        Assert.True(allowed);
    }

    // ── Null guards ───────────────────────────────────────────────────────────

    [Fact]
    public void ToolExecutor_NullRegistry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ToolExecutor(null!));
    }

    [Fact]
    public async Task ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        var (executor, _) = BuildExecutor();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteAsync(null!, new AllowAllPolicy()));
    }

    [Fact]
    public async Task ExecuteAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var (executor, _) = BuildExecutor();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => executor.ExecuteAsync(MakeContext("echo"), null!));
    }
}
