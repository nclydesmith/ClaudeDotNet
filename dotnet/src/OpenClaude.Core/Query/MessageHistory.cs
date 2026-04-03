namespace OpenClaude.Core.Query;

/// <summary>
/// Maintains an ordered list of <see cref="ChatMessage"/> values and enforces a
/// token-count ceiling by trimming the oldest messages when the estimate exceeds
/// the configured context-window limit.
/// </summary>
public sealed class MessageHistory
{
    private readonly List<ChatMessage> _messages;

    /// <param name="initialMessages">
    /// Optional seed messages (e.g. from a resumed session).
    /// </param>
    public MessageHistory(IEnumerable<ChatMessage>? initialMessages = null)
    {
        _messages = initialMessages is null
            ? []
            : [..initialMessages];
    }

    /// <summary>Appends a message to the end of the history.</summary>
    public void Add(ChatMessage message) => _messages.Add(message);

    /// <summary>
    /// Returns a snapshot of the current message history.
    /// The returned list is independent of the internal store — subsequent
    /// mutations to this <see cref="MessageHistory"/> are not reflected in it.
    /// </summary>
    public IReadOnlyList<ChatMessage> GetAll() => _messages.ToArray();

    /// <summary>Number of messages currently stored.</summary>
    public int Count => _messages.Count;

    /// <summary>
    /// Removes the oldest messages until the estimated token count falls at or
    /// below <paramref name="maxTokens"/>.  At least one message is always kept
    /// so the loop cannot become empty.
    /// </summary>
    /// <param name="maxTokens">Target token ceiling.</param>
    public void Trim(int maxTokens)
    {
        while (_messages.Count > 1 && EstimateTokens() > maxTokens)
            _messages.RemoveAt(0);
    }

    /// <summary>
    /// Rough token estimate: total character count of all text content divided by 4
    /// (the commonly used approximation for English prose).
    /// </summary>
    public int EstimateTokens()
    {
        var totalChars = 0;
        foreach (var msg in _messages)
        {
            foreach (var block in msg.Content)
            {
                totalChars += block switch
                {
                    TextBlock t => t.Text.Length,
                    ToolUseBlock tu => tu.Name.Length + tu.InputJson.Length,
                    ToolResultBlock tr => tr.Content.Length,
                    _ => 0,
                };
            }
        }

        return totalChars / 4;
    }
}
