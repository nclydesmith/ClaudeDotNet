namespace DotnetClaude.Cli.Repl;

/// <summary>
/// Tracks and formats per-turn token usage for display after each response.
/// </summary>
public sealed class StatusLine
{
    /// <summary>Prompt tokens reported by the provider for the last turn.</summary>
    public int PromptTokens { get; private set; }

    /// <summary>Completion tokens reported by the provider for the last turn.</summary>
    public int CompletionTokens { get; private set; }

    /// <summary>Updates the tracked usage figures.</summary>
    public void Update(int promptTokens, int completionTokens)
    {
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
    }

    /// <summary>
    /// Returns a formatted status string, for example:
    /// <c>tokens — prompt: 123  completion: 45</c>
    /// </summary>
    public string Format() =>
        $"tokens — prompt: {PromptTokens}  completion: {CompletionTokens}";
}
