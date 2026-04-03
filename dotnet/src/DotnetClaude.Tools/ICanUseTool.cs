namespace DotnetClaude.Tools;

/// <summary>
/// Decides whether a specific tool invocation is permitted before execution begins.
/// Implement this interface to enforce user confirmation, rate limits, or quota checks.
/// </summary>
public interface ICanUseTool
{
    /// <summary>
    /// Returns <see langword="true"/> when the named tool may be invoked with the given input,
    /// or <see langword="false"/> to prevent execution.
    /// </summary>
    /// <param name="toolName">The registered name of the tool to be executed.</param>
    /// <param name="inputJson">JSON-encoded input that will be passed to the tool.</param>
    /// <param name="cancellationToken">Token to cancel the permission check.</param>
    Task<bool> IsAllowedAsync(
        string toolName,
        string inputJson,
        CancellationToken cancellationToken = default);
}
