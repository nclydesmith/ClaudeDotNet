namespace OpenClaude.Core.Query;

/// <summary>
/// Determines whether provider errors are retryable and computes exponential-backoff delays.
/// Mirrors the fix(retry) commit logic: quota-exhausted 429s are never retried; other 429s
/// and 503s are considered transient and eligible for retry.
/// </summary>
public static class RetryPolicy
{
    // Keywords that indicate hard quota exhaustion (not just rate limiting).
    // Mirrors isQuotaExhausted() in src/services/api/withRetry.ts.
    private static readonly string[] QuotaKeywords = ["limit: 0", "exceeded your current quota"];

    /// <summary>
    /// Returns <see langword="true"/> when the exception signals that the account's
    /// API quota is permanently exhausted for the current billing period.
    /// Such errors must NOT be retried.
    /// </summary>
    public static bool IsQuotaExhausted(Exception ex)
    {
        if (ex is not LlmProviderException { StatusCode: 429 } llmEx)
            return false;

        var msg = llmEx.Message.ToLowerInvariant();
        foreach (var keyword in QuotaKeywords)
        {
            if (msg.Contains(keyword, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the exception is a transient capacity error
    /// (HTTP 429 rate-limit or HTTP 503 service unavailable) that is safe to retry.
    /// Quota-exhausted 429s always return <see langword="false"/>.
    /// </summary>
    public static bool IsTransient(Exception ex)
    {
        if (ex is not LlmProviderException llmEx)
            return false;

        return llmEx.StatusCode switch
        {
            429 => !IsQuotaExhausted(ex),
            503 => true,
            _ => false,
        };
    }

    /// <summary>
    /// Returns the delay to wait before the next attempt using exponential backoff
    /// capped at 30 seconds: <c>baseDelayMs * 2^attempt</c>.
    /// </summary>
    /// <param name="attempt">Zero-based attempt index (0 = first retry).</param>
    /// <param name="baseDelayMs">Base delay in milliseconds (default 1 000 ms).</param>
    public static TimeSpan GetDelay(int attempt, int baseDelayMs = 1_000)
    {
        const double maxMs = 30_000;
        var ms = Math.Min(baseDelayMs * Math.Pow(2, attempt), maxMs);
        return TimeSpan.FromMilliseconds(ms);
    }
}
