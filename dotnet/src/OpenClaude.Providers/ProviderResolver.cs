namespace OpenClaude.Providers;

/// <summary>
/// Resolves the active LLM provider and its configuration from environment variables,
/// mirroring the TypeScript <c>providerConfig.ts</c> detection logic.
/// </summary>
public static class ProviderResolver
{
    private const string DefaultAnthropicBaseUrl = "https://api.anthropic.com";
    private const string DefaultAnthropicModel = "claude-sonnet-4-5";

    private const string DefaultOpenAiBaseUrl = "https://api.openai.com/v1";
    private const string DefaultOpenAiModel = "gpt-4o";

    // Gemini exposes an OpenAI-compatible endpoint at this base URL.
    private const string GeminiOpenAiCompatBaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai";
    private const string DefaultGeminiModel = "gemini-2.5-pro";

    private const string DefaultOllamaModel = "llama3";

    /// <summary>
    /// Resolves provider configuration from environment variables.
    /// </summary>
    /// <remarks>
    /// Detection order (first match wins):
    /// <list type="number">
    ///   <item><c>CLAUDE_CODE_USE_OPENAI</c> → OpenAI-compatible using <c>OPENAI_*</c> vars.</item>
    ///   <item><c>GEMINI_API_KEY</c> → Gemini via its OpenAI-compatible endpoint.</item>
    ///   <item><c>OLLAMA_BASE_URL</c> → Local Ollama via OpenAI-compatible API.</item>
    ///   <item>Default → Anthropic (requires <c>ANTHROPIC_API_KEY</c>).</item>
    /// </list>
    /// </remarks>
    /// <param name="env">
    /// Optional environment dictionary for testing.
    /// When <see langword="null"/>, the process environment is used.
    /// </param>
    /// <returns>A fully resolved <see cref="ProviderConfig"/>.</returns>
    /// <exception cref="ProviderConfigException">
    /// Thrown when no valid provider configuration can be determined from the environment.
    /// </exception>
    public static ProviderConfig Resolve(IReadOnlyDictionary<string, string?>? env = null)
    {
        var e = env ?? GetProcessEnv();

        // 1. CLAUDE_CODE_USE_OPENAI → force OpenAI-compatible
        if (IsEnvTruthy(e, "CLAUDE_CODE_USE_OPENAI"))
            return ResolveOpenAiCompatible(e);

        // 2. GEMINI_API_KEY → Gemini via OpenAI-compatible endpoint
        var geminiKey = GetValue(e, "GEMINI_API_KEY");
        if (geminiKey is not null)
            return ResolveGemini(geminiKey, e);

        // 3. OLLAMA_BASE_URL → local Ollama
        var ollamaUrl = GetValue(e, "OLLAMA_BASE_URL");
        if (ollamaUrl is not null)
            return ResolveOllama(ollamaUrl, e);

        // 4. Default: Anthropic
        return ResolveAnthropic(e);
    }

    private static ProviderConfig ResolveAnthropic(IReadOnlyDictionary<string, string?> e)
    {
        var apiKey = GetValue(e, "ANTHROPIC_API_KEY");
        if (apiKey is null)
            throw new ProviderConfigException(
                "No LLM provider configuration found. " +
                "Set ANTHROPIC_API_KEY for Anthropic (Claude), or configure an OpenAI-compatible " +
                "provider via CLAUDE_CODE_USE_OPENAI, GEMINI_API_KEY, or OLLAMA_BASE_URL.");

        var model = GetValue(e, "ANTHROPIC_MODEL") ?? DefaultAnthropicModel;
        return new ProviderConfig(ProviderType.Anthropic, DefaultAnthropicBaseUrl, model, apiKey);
    }

    private static ProviderConfig ResolveOpenAiCompatible(IReadOnlyDictionary<string, string?> e)
    {
        var baseUrl = GetValue(e, "OPENAI_BASE_URL")
            ?? GetValue(e, "OPENAI_API_BASE")
            ?? DefaultOpenAiBaseUrl;

        var model = GetValue(e, "OPENAI_MODEL") ?? DefaultOpenAiModel;
        var apiKey = GetValue(e, "OPENAI_API_KEY") ?? string.Empty;

        return new ProviderConfig(ProviderType.OpenAiCompatible, TrimSlash(baseUrl), model, apiKey);
    }

    private static ProviderConfig ResolveGemini(string geminiKey, IReadOnlyDictionary<string, string?> e)
    {
        // Honour an explicit OPENAI_BASE_URL override; otherwise use the Gemini compat endpoint.
        var baseUrl = GetValue(e, "OPENAI_BASE_URL") ?? GeminiOpenAiCompatBaseUrl;
        var model = GetValue(e, "OPENAI_MODEL") ?? DefaultGeminiModel;
        // Prefer an explicit OPENAI_API_KEY; fall back to the Gemini key.
        var apiKey = GetValue(e, "OPENAI_API_KEY") ?? geminiKey;

        return new ProviderConfig(ProviderType.OpenAiCompatible, TrimSlash(baseUrl), model, apiKey);
    }

    private static ProviderConfig ResolveOllama(string ollamaUrl, IReadOnlyDictionary<string, string?> e)
    {
        // Ollama exposes an OpenAI-compatible /v1 sub-path.
        var baseUrl = TrimSlash(ollamaUrl) + "/v1";
        var model = GetValue(e, "OPENAI_MODEL") ?? GetValue(e, "OLLAMA_MODEL") ?? DefaultOllamaModel;
        var apiKey = GetValue(e, "OPENAI_API_KEY") ?? string.Empty;

        return new ProviderConfig(ProviderType.OpenAiCompatible, baseUrl, model, apiKey);
    }

    /// <summary>Returns the trimmed, non-empty value for <paramref name="key"/>, or <see langword="null"/>.</summary>
    private static string? GetValue(IReadOnlyDictionary<string, string?> e, string key) =>
        e.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    /// <summary>Returns <see langword="true"/> when the env var is set to a truthy value (1, true, yes).</summary>
    private static bool IsEnvTruthy(IReadOnlyDictionary<string, string?> e, string key)
    {
        var value = GetValue(e, key);
        return value?.ToLowerInvariant() is "1" or "true" or "yes";
    }

    private static string TrimSlash(string url) => url.TrimEnd('/');

    private static Dictionary<string, string?> GetProcessEnv()
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var envVars = Environment.GetEnvironmentVariables();
        foreach (string key in envVars.Keys.Cast<string>())
            dict[key] = envVars[key] as string;
        return dict;
    }
}
