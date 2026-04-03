namespace OpenClaude.Core.Query;

/// <summary>
/// Configures the behaviour of a <see cref="QueryEngine"/> run.
/// </summary>
public sealed record QueryOptions
{
    /// <summary>Maximum number of retry attempts for transient provider errors.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Estimated token budget for message history.
    /// When history exceeds this limit the oldest messages are trimmed.
    /// </summary>
    public int MaxContextTokens { get; init; } = 100_000;

    /// <summary>Base delay in milliseconds for the first retry (doubles on each attempt).</summary>
    public int RetryBaseDelayMs { get; init; } = 1_000;

    /// <summary>Optional system prompt prepended to every request.</summary>
    public string? SystemPrompt { get; init; }
}
