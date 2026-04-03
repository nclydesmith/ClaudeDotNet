namespace OpenClaude.Tools;

/// <summary>
/// Default implementation of <see cref="IToolExecutor"/>.
/// Resolves tools from a <see cref="ToolRegistry"/>, enforces a permission policy,
/// and emits progress events to the <see cref="IToolProgressSink"/> in the execution context.
/// </summary>
public sealed class ToolExecutor : IToolExecutor
{
    private readonly ToolRegistry _registry;

    /// <summary>Initialises the executor with the registry used to resolve tools.</summary>
    /// <param name="registry">The tool registry to look up tools by name.</param>
    public ToolExecutor(ToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        ICanUseTool permissionPolicy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(permissionPolicy);

        bool allowed = await permissionPolicy.IsAllowedAsync(context.ToolName, context.InputJson, cancellationToken);
        if (!allowed)
            return ToolResult.Denied($"Permission denied for tool '{context.ToolName}'.");

        await context.ProgressSink.ReportAsync(
            new ToolStartedEvent(context.ToolName, context.InputJson),
            cancellationToken);

        ITool tool = _registry.Resolve(context.ToolName);
        ToolResult result = await tool.ExecuteAsync(context, cancellationToken);

        await context.ProgressSink.ReportAsync(
            new ToolCompletedEvent(context.ToolName, result),
            cancellationToken);

        return result;
    }
}
