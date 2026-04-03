namespace DotnetClaude.Tools;

/// <summary>
/// Composes multiple <see cref="ICanUseTool"/> checks into a single policy.
/// All registered checks must allow a call for it to be permitted (AND semantics).
/// </summary>
public sealed class ToolPermissionPolicy : ICanUseTool
{
    private readonly IReadOnlyList<ICanUseTool> _checks;

    /// <summary>Initialises a policy that always allows every tool call.</summary>
    public ToolPermissionPolicy() => _checks = [];

    /// <summary>Initialises a policy composed of the given permission checks.</summary>
    /// <param name="checks">Ordered list of checks; first denial short-circuits evaluation.</param>
    public ToolPermissionPolicy(IReadOnlyList<ICanUseTool> checks) => _checks = checks;

    /// <inheritdoc/>
    public async Task<bool> IsAllowedAsync(
        string toolName,
        string inputJson,
        CancellationToken cancellationToken = default)
    {
        foreach (ICanUseTool check in _checks)
        {
            if (!await check.IsAllowedAsync(toolName, inputJson, cancellationToken))
                return false;
        }
        return true;
    }
}

/// <summary>
/// A simple rate-limiting permission check: allows at most <c>maxCallsPerWindow</c>
/// invocations of the same tool within the specified sliding time window.
/// </summary>
public sealed class RateLimitingPolicy : ICanUseTool
{
    private readonly int _maxCallsPerWindow;
    private readonly TimeSpan _window;
    private readonly Dictionary<string, Queue<DateTimeOffset>> _callLog = [];

    /// <summary>
    /// Initialises a rate-limiting policy.
    /// </summary>
    /// <param name="maxCallsPerWindow">Maximum calls allowed within <paramref name="window"/>.</param>
    /// <param name="window">The sliding time window to measure calls against.</param>
    public RateLimitingPolicy(int maxCallsPerWindow, TimeSpan window)
    {
        _maxCallsPerWindow = maxCallsPerWindow;
        _window = window;
    }

    /// <inheritdoc/>
    public Task<bool> IsAllowedAsync(
        string toolName,
        string inputJson,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (!_callLog.TryGetValue(toolName, out Queue<DateTimeOffset>? timestamps))
        {
            timestamps = new Queue<DateTimeOffset>();
            _callLog[toolName] = timestamps;
        }

        // Evict calls outside the sliding window.
        while (timestamps.Count > 0 && now - timestamps.Peek() > _window)
            timestamps.Dequeue();

        if (timestamps.Count >= _maxCallsPerWindow)
            return Task.FromResult(false);

        timestamps.Enqueue(now);
        return Task.FromResult(true);
    }
}
