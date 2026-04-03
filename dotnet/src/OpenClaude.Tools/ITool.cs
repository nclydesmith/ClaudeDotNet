namespace OpenClaude.Tools;

/// <summary>
/// Represents a single executable tool that can be discovered, registered, and invoked
/// by the tool execution pipeline.
/// </summary>
public interface ITool
{
    /// <summary>
    /// The unique name used to look up this tool in <see cref="ToolRegistry"/>.
    /// Names are case-sensitive and must be stable across registrations.
    /// </summary>
    string Name { get; }

    /// <summary>A short human-readable description of what the tool does.</summary>
    string Description { get; }

    /// <summary>
    /// Executes the tool using the provided context and returns its result.
    /// </summary>
    /// <param name="context">Execution context including input JSON and a progress sink.</param>
    /// <param name="cancellationToken">Token to cancel the execution.</param>
    /// <returns>A <see cref="ToolResult"/> describing success, error, or denial.</returns>
    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);
}
