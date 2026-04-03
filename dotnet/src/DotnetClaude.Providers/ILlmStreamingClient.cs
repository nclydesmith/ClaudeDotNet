namespace DotnetClaude.Providers;

/// <summary>
/// Provides streaming text generation from an LLM endpoint.
/// </summary>
public interface ILlmStreamingClient
{
    /// <summary>
    /// Streams text response tokens for the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt text.</param>
    /// <param name="cancellationToken">Token to cancel the streaming operation.</param>
    IAsyncEnumerable<string> StreamTextAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
