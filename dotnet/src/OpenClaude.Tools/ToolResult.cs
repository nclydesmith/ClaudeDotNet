namespace OpenClaude.Tools;

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
    /// <summary>Creates a successful result with the given output string.</summary>
    public static ToolResult Succeeded(string output) =>
        new(ToolResultStatus.Success, output);

    /// <summary>Creates a denied result with a human-readable reason.</summary>
    public static ToolResult Denied(string reason) =>
        new(ToolResultStatus.Denied, Output: null, ErrorMessage: reason);

    /// <summary>Creates an error result with a human-readable error message.</summary>
    public static ToolResult Error(string message) =>
        new(ToolResultStatus.Error, Output: null, ErrorMessage: message);
}
