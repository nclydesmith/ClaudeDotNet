namespace DotnetClaude.Providers;

/// <summary>
/// Discriminates between supported LLM provider backends.
/// </summary>
public enum ProviderType
{
    /// <summary>Anthropic API (Claude models).</summary>
    Anthropic,

    /// <summary>OpenAI-compatible API (OpenAI, DeepSeek, Groq, Mistral, Ollama, Gemini, etc.).</summary>
    OpenAiCompatible,
}

/// <summary>
/// Resolved provider configuration derived from environment variables and profile settings.
/// </summary>
/// <param name="ProviderType">The selected provider backend.</param>
/// <param name="BaseUrl">The API base URL (trailing slashes stripped).</param>
/// <param name="Model">The model name to use for requests.</param>
/// <param name="ApiKey">The API key; may be empty for local providers such as Ollama.</param>
public sealed record ProviderConfig(
    ProviderType ProviderType,
    string BaseUrl,
    string Model,
    string ApiKey);

/// <summary>
/// Thrown when provider configuration is missing, ambiguous, or unsupported.
/// </summary>
public sealed class ProviderConfigException : Exception
{
    public ProviderConfigException(string message) : base(message) { }

    public ProviderConfigException(string message, Exception inner) : base(message, inner) { }
}
