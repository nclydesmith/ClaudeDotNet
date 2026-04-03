using DotnetClaude.Providers;

namespace DotnetClaude.Core.Tests.Providers;

/// <summary>
/// Unit tests for <see cref="ProviderResolver.Resolve"/>.
/// All tests inject an environment dictionary — no process env vars or live API calls.
/// </summary>
public sealed class ProviderResolverTests
{
    // -----------------------------------------------------------------------
    // AC1: ProviderResolver.Resolve() returns the correct provider type
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("yes")]
    public void Resolve_ClaudeCodeUseOpenAi_ReturnsOpenAiCompatible(string envValue)
    {
        var env = Env(("CLAUDE_CODE_USE_OPENAI", envValue), ("OPENAI_API_KEY", "sk-test"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal(ProviderType.OpenAiCompatible, config.ProviderType);
    }

    [Fact]
    public void Resolve_GeminiApiKey_ReturnsOpenAiCompatible()
    {
        var env = Env(("GEMINI_API_KEY", "gemini-abc123"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal(ProviderType.OpenAiCompatible, config.ProviderType);
    }

    [Fact]
    public void Resolve_OllamaBaseUrl_ReturnsOpenAiCompatible()
    {
        var env = Env(("OLLAMA_BASE_URL", "http://localhost:11434"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal(ProviderType.OpenAiCompatible, config.ProviderType);
    }

    [Fact]
    public void Resolve_DefaultWithAnthropicApiKey_ReturnsAnthropic()
    {
        var env = Env(("ANTHROPIC_API_KEY", "sk-ant-test123"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal(ProviderType.Anthropic, config.ProviderType);
    }

    // -----------------------------------------------------------------------
    // AC3: Config reads OPENAI_BASE_URL, OPENAI_MODEL, and OPENAI_API_KEY
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_OpenAiEnvVars_ReadsBaseUrlModelAndApiKey()
    {
        var env = Env(
            ("CLAUDE_CODE_USE_OPENAI", "1"),
            ("OPENAI_BASE_URL", "https://api.example.com/v1"),
            ("OPENAI_MODEL", "gpt-4-turbo"),
            ("OPENAI_API_KEY", "sk-custom-key"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("https://api.example.com/v1", config.BaseUrl);
        Assert.Equal("gpt-4-turbo", config.Model);
        Assert.Equal("sk-custom-key", config.ApiKey);
    }

    [Fact]
    public void Resolve_OpenAiApiBaseFallback_UsesApiBaseUrl()
    {
        // OPENAI_API_BASE is the legacy alias for OPENAI_BASE_URL
        var env = Env(
            ("CLAUDE_CODE_USE_OPENAI", "1"),
            ("OPENAI_API_BASE", "https://legacy.example.com/v1"),
            ("OPENAI_API_KEY", "sk-key"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("https://legacy.example.com/v1", config.BaseUrl);
    }

    [Fact]
    public void Resolve_OllamaUrl_AppendV1Suffix()
    {
        var env = Env(("OLLAMA_BASE_URL", "http://localhost:11434"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("http://localhost:11434/v1", config.BaseUrl);
    }

    [Fact]
    public void Resolve_OllamaUrl_StripsTrailingSlash()
    {
        var env = Env(("OLLAMA_BASE_URL", "http://localhost:11434/"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("http://localhost:11434/v1", config.BaseUrl);
    }

    [Fact]
    public void Resolve_GeminiKey_UsesGeminiOpenAiCompatEndpointByDefault()
    {
        var env = Env(("GEMINI_API_KEY", "gemini-key"));

        var config = ProviderResolver.Resolve(env);

        Assert.Contains("generativelanguage.googleapis.com", config.BaseUrl);
        Assert.Equal("gemini-key", config.ApiKey);
    }

    [Fact]
    public void Resolve_GeminiKeyWithOpenAiBaseUrl_HonoursOverride()
    {
        var env = Env(
            ("GEMINI_API_KEY", "gemini-key"),
            ("OPENAI_BASE_URL", "https://custom-gemini.example.com/v1"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("https://custom-gemini.example.com/v1", config.BaseUrl);
    }

    [Fact]
    public void Resolve_AnthropicConfig_UsesAnthropicBaseUrl()
    {
        var env = Env(("ANTHROPIC_API_KEY", "sk-ant-abc"));

        var config = ProviderResolver.Resolve(env);

        Assert.Equal("https://api.anthropic.com", config.BaseUrl);
        Assert.Equal("sk-ant-abc", config.ApiKey);
    }

    // -----------------------------------------------------------------------
    // AC4: Unsupported / missing config throws ProviderConfigException
    // -----------------------------------------------------------------------

    [Fact]
    public void Resolve_EmptyEnvironment_ThrowsProviderConfigException()
    {
        var env = new Dictionary<string, string?>();

        var ex = Assert.Throws<ProviderConfigException>(() => ProviderResolver.Resolve(env));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void Resolve_WhitespaceOnlyAnthropicKey_ThrowsProviderConfigException()
    {
        var env = Env(("ANTHROPIC_API_KEY", "   "));

        Assert.Throws<ProviderConfigException>(() => ProviderResolver.Resolve(env));
    }

    [Fact]
    public void ProviderConfigException_IsException()
    {
        // Verify the type hierarchy
        var ex = new ProviderConfigException("test message");
        Assert.IsAssignableFrom<Exception>(ex);
        Assert.Equal("test message", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Providers implement ILlmProvider
    // -----------------------------------------------------------------------

    [Fact]
    public void AnthropicProvider_ImplementsILlmProvider()
    {
        ILlmProvider provider = new AnthropicProvider();
        Assert.Equal(ProviderType.Anthropic, provider.ProviderType);
        Assert.Equal("Anthropic", provider.Name);
    }

    [Fact]
    public void OpenAiCompatibleProvider_ImplementsILlmProvider()
    {
        ILlmProvider provider = new OpenAiCompatibleProvider();
        Assert.Equal(ProviderType.OpenAiCompatible, provider.ProviderType);
        Assert.Equal("OpenAI-Compatible", provider.Name);
    }

    [Fact]
    public void AnthropicProvider_CreateStreamingClient_ReturnsILlmStreamingClient()
    {
        var provider = new AnthropicProvider();
        var config = new ProviderConfig(ProviderType.Anthropic, "https://api.anthropic.com", "claude-sonnet-4-5", "sk-ant-test");

        var client = provider.CreateStreamingClient(config);

        Assert.IsAssignableFrom<ILlmStreamingClient>(client);
    }

    [Fact]
    public void OpenAiCompatibleProvider_CreateStreamingClient_ReturnsILlmStreamingClient()
    {
        var provider = new OpenAiCompatibleProvider();
        var config = new ProviderConfig(ProviderType.OpenAiCompatible, "https://api.openai.com/v1", "gpt-4o", "sk-test");

        var client = provider.CreateStreamingClient(config);

        Assert.IsAssignableFrom<ILlmStreamingClient>(client);
    }

    [Fact]
    public void AnthropicProvider_CreateStreamingClient_WrongProviderType_Throws()
    {
        var provider = new AnthropicProvider();
        var config = new ProviderConfig(ProviderType.OpenAiCompatible, "https://api.openai.com/v1", "gpt-4o", "sk-test");

        Assert.Throws<ProviderConfigException>(() => provider.CreateStreamingClient(config));
    }

    [Fact]
    public void OpenAiCompatibleProvider_CreateStreamingClient_WrongProviderType_Throws()
    {
        var provider = new OpenAiCompatibleProvider();
        var config = new ProviderConfig(ProviderType.Anthropic, "https://api.anthropic.com", "claude-sonnet-4-5", "sk-ant-test");

        Assert.Throws<ProviderConfigException>(() => provider.CreateStreamingClient(config));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Dictionary<string, string?> Env(params (string Key, string? Value)[] pairs)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
            dict[key] = value;
        return dict;
    }
}
