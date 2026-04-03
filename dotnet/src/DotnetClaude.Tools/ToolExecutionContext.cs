namespace DotnetClaude.Tools;

/// <summary>
/// Carries all contextual data available to a tool during a single execution call,
/// including the encoded input and a sink for streaming progress events.
/// </summary>
public sealed record ToolExecutionContext(
    /// <summary>The registered name of the tool being executed.</summary>
    string ToolName,

    /// <summary>JSON-encoded parameters to pass to the tool.</summary>
    string InputJson,

    /// <summary>Sink that receives progress events emitted during execution.</summary>
    IToolProgressSink ProgressSink);
