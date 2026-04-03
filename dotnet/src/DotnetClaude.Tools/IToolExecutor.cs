namespace DotnetClaude.Tools;

/// <summary>
/// Orchestrates a single tool execution: checks permission, emits progress events,
/// and delegates to the registered <see cref="ITool"/> implementation.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes the tool named in <paramref name="context"/>, subject to the given permission policy.
    /// </summary>
    /// <remarks>
    /// If <paramref name="permissionPolicy"/> denies the call, the tool is never invoked and a
    /// <see cref="ToolResult"/> with <see cref="ToolResultStatus.Denied"/> is returned immediately.
    /// Otherwise, <see cref="ToolProgressEvent"/>s are emitted to <see cref="ToolExecutionContext.ProgressSink"/>
    /// before and after execution.
    /// </remarks>
    /// <param name="context">Execution context (tool name, input JSON, progress sink).</param>
    /// <param name="permissionPolicy">Policy consulted before the tool is invoked.</param>
    /// <param name="cancellationToken">Token to cancel the entire operation.</param>
    /// <returns>The <see cref="ToolResult"/> produced by the tool, or a denied/error result.</returns>
    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        ICanUseTool permissionPolicy,
        CancellationToken cancellationToken = default);
}
