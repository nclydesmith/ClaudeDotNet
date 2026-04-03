namespace OpenClaude.Tools;

/// <summary>
/// Receives progress events emitted by <see cref="IToolExecutor"/> during tool execution.
/// Implementations may log, stream, or aggregate events for callers.
/// </summary>
public interface IToolProgressSink
{
    /// <summary>
    /// Reports a single progress event asynchronously.
    /// </summary>
    /// <param name="progressEvent">The event to report.</param>
    /// <param name="cancellationToken">Token to cancel the report operation.</param>
    ValueTask ReportAsync(ToolProgressEvent progressEvent, CancellationToken cancellationToken = default);
}

/// <summary>Discriminated union base for all tool execution progress events.</summary>
public abstract record ToolProgressEvent;

/// <summary>Emitted immediately before a tool is invoked.</summary>
public sealed record ToolStartedEvent(string ToolName, string InputJson) : ToolProgressEvent;

/// <summary>Emitted after a tool finishes execution (success, error, or denied).</summary>
public sealed record ToolCompletedEvent(string ToolName, ToolResult Result) : ToolProgressEvent;

/// <summary>An intermediate status message emitted by a long-running tool.</summary>
public sealed record ToolProgressMessage(string ToolName, string Message) : ToolProgressEvent;
