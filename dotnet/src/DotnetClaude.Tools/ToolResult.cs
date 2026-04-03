namespace DotnetClaude.Tools;

/// <summary>The outcome status of a tool execution attempt.</summary>
public enum ToolResultStatus
{
    /// <summary>The tool executed successfully and produced output.</summary>
    Success,

    /// <summary>Execution was blocked by the permission policy before the tool was called.</summary>
    Denied,

    /// <summary>The tool threw an exception or returned an error during execution.</summary>
    Error,
}

/// <summary>
/// Represents the result of a single tool execution, including status and optional output or error information.
/// </summary>
public sealed record ToolResult(ToolResultStatus Status, string? Output, string? ErrorMessage = null)
{
    /// <summary>Structured content returned by the tool (alias for <see cref="Output"/>).</summary>
    public string? Content => Output;

    /// <summary>Whether the tool execution resulted in an error.</summary>
    public bool IsError => Status == ToolResultStatus.Error;

    /// <summary>Optional key-value metadata attached by the tool.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Creates a successful result with the given output string.</summary>
    public static ToolResult Succeeded(string output) =>
        new(ToolResultStatus.Success, output);

    /// <summary>Creates a successful result with output and metadata.</summary>
    public static ToolResult Succeeded(string output, IReadOnlyDictionary<string, string> metadata) =>
        new(ToolResultStatus.Success, output) { Metadata = metadata };

    /// <summary>Creates a denied result with a human-readable reason.</summary>
    public static ToolResult Denied(string reason) =>
        new(ToolResultStatus.Denied, Output: null, ErrorMessage: reason);

    /// <summary>Creates an error result with a human-readable error message.</summary>
    public static ToolResult Error(string message) =>
        new(ToolResultStatus.Error, Output: null, ErrorMessage: message);
}
