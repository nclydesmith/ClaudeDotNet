using OpenClaude.Tools;

namespace OpenClaude.Core.Tests.Tools;

public sealed class ToolRegistryTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ITool MakeTool(string name, string description = "test tool") =>
        new StubTool(name, description);

    private sealed class StubTool(string name, string description) : ITool
    {
        public string Name { get; } = name;
        public string Description { get; } = description;

        public Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(ToolResult.Succeeded("ok"));
    }

    // ── Register / Resolve ────────────────────────────────────────────────────

    [Fact]
    public void Register_ThenResolve_ReturnsRegisteredTool()
    {
        var registry = new ToolRegistry();
        var tool = MakeTool("my_tool");

        registry.Register(tool);
        ITool resolved = registry.Resolve("my_tool");

        Assert.Same(tool, resolved);
    }

    [Fact]
    public void Register_DuplicateName_ThrowsDuplicateToolException()
    {
        var registry = new ToolRegistry();
        registry.Register(MakeTool("dup"));

        var ex = Assert.Throws<DuplicateToolException>(() => registry.Register(MakeTool("dup")));
        Assert.Equal("dup", ex.ToolName);
    }

    [Fact]
    public void Resolve_UnknownName_ThrowsToolNotFoundException()
    {
        var registry = new ToolRegistry();

        var ex = Assert.Throws<ToolNotFoundException>(() => registry.Resolve("ghost"));
        Assert.Equal("ghost", ex.ToolName);
    }

    [Fact]
    public void Register_NullTool_ThrowsArgumentNullException()
    {
        var registry = new ToolRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_NoTools_ReturnsEmpty()
    {
        var registry = new ToolRegistry();

        Assert.Empty(registry.GetAll());
    }

    [Fact]
    public void GetAll_AfterRegistrations_ReturnsAllTools()
    {
        var registry = new ToolRegistry();
        var a = MakeTool("tool_a");
        var b = MakeTool("tool_b");

        registry.Register(a);
        registry.Register(b);

        IReadOnlyCollection<ITool> all = registry.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Contains(a, all);
        Assert.Contains(b, all);
    }

    // ── Name case-sensitivity ─────────────────────────────────────────────────

    [Fact]
    public void Resolve_IsCaseSensitive()
    {
        var registry = new ToolRegistry();
        registry.Register(MakeTool("MyTool"));

        Assert.Throws<ToolNotFoundException>(() => registry.Resolve("mytool"));
    }
}
