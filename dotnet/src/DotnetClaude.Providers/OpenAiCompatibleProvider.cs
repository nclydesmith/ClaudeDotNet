namespace DotnetClaude.Providers;

/// <summary>
/// LLM provider implementation for OpenAI-compatible APIs.
/// Covers OpenAI, DeepSeek, Groq, Mistral, Ollama, Gemini (via compat endpoint), and others.
/// </summary>
/// <remarks>
/// Streaming is delegated to the OpenAI .NET SDK via its chat-completions endpoint.
/// TODO: Integrate OpenAI NuGet package when the query engine is implemented (SUB-003).
/// </remarks>
public sealed class OpenAiCompatibleProvider : ILlmProvider
{
    /// <inheritdoc />
    public string Name => "OpenAI-Compatible";

    /// <inheritdoc />
    public ProviderType ProviderType => ProviderType.OpenAiCompatible;

    /// <inheritdoc />
    public ILlmStreamingClient CreateStreamingClient(ProviderConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.ProviderType != ProviderType.OpenAiCompatible)
            throw new ProviderConfigException(
                $"OpenAiCompatibleProvider requires a config with ProviderType.OpenAiCompatible, " +
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
                $"OpenAiCompatibleProvider streaming requires OpenAI SDK integration " +
                $"(BaseUrl={_config.BaseUrl}, Model={_config.Model}). " +
                "This will be completed in SUB-003.");
    }
}
