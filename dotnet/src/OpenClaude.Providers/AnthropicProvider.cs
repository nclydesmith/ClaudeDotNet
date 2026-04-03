namespace OpenClaude.Providers;

/// <summary>
/// LLM provider implementation for the Anthropic API (Claude models).
/// </summary>
/// <remarks>
/// Streaming is delegated to the Anthropic .NET SDK.
/// TODO: Integrate Anthropic.SDK NuGet package when the query engine is implemented (SUB-003).
/// </remarks>
public sealed class AnthropicProvider : ILlmProvider
{
    /// <inheritdoc />
    public string Name => "Anthropic";

    /// <inheritdoc />
    public ProviderType ProviderType => ProviderType.Anthropic;

    /// <inheritdoc />
    public ILlmStreamingClient CreateStreamingClient(ProviderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.ProviderType != ProviderType.Anthropic)
            throw new ProviderConfigException(
                $"AnthropicProvider requires a config with ProviderType.Anthropic, " +
                $"but received '{config.ProviderType}'.");

        return new StreamingClient(config);
    }

    private sealed class StreamingClient : ILlmStreamingClient
    {
        private readonly ProviderConfig _config;

        internal StreamingClient(ProviderConfig config) => _config = config;

        public IAsyncEnumerable<string> StreamTextAsync(
            string prompt,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException(
                $"AnthropicProvider streaming requires Anthropic SDK integration " +
                $"(BaseUrl={_config.BaseUrl}, Model={_config.Model}). " +
                "This will be completed in SUB-003.");
    }
}
