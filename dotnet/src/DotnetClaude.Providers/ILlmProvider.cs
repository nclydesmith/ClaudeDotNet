namespace DotnetClaude.Providers;

/// <summary>
/// Represents an LLM provider capable of creating streaming clients.
/// </summary>
public interface ILlmProvider
{
    /// <summary>Gets the display name of this provider.</summary>
    string Name { get; }

    /// <summary>Gets the provider type discriminator.</summary>
    ProviderType ProviderType { get; }

    /// <summary>
    /// Creates a streaming client configured with the resolved provider settings.
    /// </summary>
    /// <param name="config">Resolved provider configuration.</param>
    /// <returns>A configured <see cref="ILlmStreamingClient"/> instance.</returns>
    ILlmStreamingClient CreateStreamingClient(ProviderConfig config);
}
