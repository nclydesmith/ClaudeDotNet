using System.Runtime.CompilerServices;

namespace OpenClaude.Core.Query;

/// <summary>
/// Orchestrates the multi-turn query/response loop against an LLM provider.
///
/// <para>
/// Each call to <see cref="RunAsync"/> streams <see cref="QueryChunk"/> values:
/// <list type="bullet">
///   <item><description><see cref="TextQueryChunk"/> – streamed text tokens from the model.</description></item>
///   <item><description><see cref="ToolUseQueryChunk"/> – the engine is about to run a tool.</description></item>
///   <item><description><see cref="ToolResultQueryChunk"/> – the tool result appended to history.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Transient provider errors (HTTP 429 rate-limit, HTTP 503) are retried with
/// exponential backoff up to <see cref="QueryOptions.MaxRetryAttempts"/> times.
/// Quota-exhausted 429s are <em>not</em> retried and surface immediately as a
/// <see cref="LlmProviderException"/>.
/// </para>
/// </summary>
public sealed class QueryEngine
{
    private readonly ILlmChatClient _chatClient;
    private readonly IToolExecutor _toolExecutor;
    private readonly QueryOptions _options;

    public QueryEngine(
        ILlmChatClient chatClient,
        IToolExecutor toolExecutor,
        QueryOptions options)
    {
        _chatClient = chatClient;
        _toolExecutor = toolExecutor;
        _options = options;
    }

    /// <summary>
    /// Runs the query loop for the provided conversation history, yielding
    /// <see cref="QueryChunk"/> values as the model responds and tools are executed.
    /// </summary>
    /// <param name="initialMessages">
    /// The conversation history to start from. The engine appends new messages
    /// internally; the caller's list is not mutated.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async IAsyncEnumerable<QueryChunk> RunAsync(
        IReadOnlyList<ChatMessage> initialMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = new MessageHistory(initialMessages);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fetch all chunks for this turn, retrying on transient errors.
            var turnChunks = await FetchWithRetryAsync(history, cancellationToken)
                .ConfigureAwait(false);

            // Process chunks: build the assistant message and collect tool uses.
            var assistantBlocks = new List<ContentBlock>();
            var toolUses = new List<ToolUseLlmChunk>();

            foreach (var chunk in turnChunks)
            {
                if (chunk is TextDeltaLlmChunk { Delta: var delta })
                {
                    assistantBlocks.Add(new TextBlock(delta));
                    yield return new TextQueryChunk(delta);
                }
                else if (chunk is ToolUseLlmChunk toolUse)
                {
                    assistantBlocks.Add(new ToolUseBlock(toolUse.Id, toolUse.Name, toolUse.InputJson));
                    toolUses.Add(toolUse);
                    yield return new ToolUseQueryChunk(toolUse.Id, toolUse.Name, toolUse.InputJson);
                }
            }

            // Append assistant turn to history.
            history.Add(ChatMessage.Assistant(assistantBlocks));

            // No tool calls → conversation is complete.
            if (toolUses.Count == 0)
                break;

            // Execute all tools and build the tool-result user message.
            var resultBlocks = new List<ContentBlock>(toolUses.Count);
            foreach (var toolUse in toolUses)
            {
                string result;
                bool isError;
                try
                {
                    result = await _toolExecutor.ExecuteAsync(
                        toolUse.Name, toolUse.InputJson, cancellationToken).ConfigureAwait(false);
                    isError = false;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result = ex.Message;
                    isError = true;
                }

                resultBlocks.Add(new ToolResultBlock(toolUse.Id, result, isError));
                yield return new ToolResultQueryChunk(toolUse.Id, result, isError);
            }

            // Append tool results and trim history if needed.
            history.Add(ChatMessage.User(resultBlocks));
            history.Trim(_options.MaxContextTokens);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Calls the chat client, collecting all streamed chunks into a list.
    /// Retries on transient errors with exponential backoff.
    /// Quota-exhausted 429s are rethrown immediately without retry.
    /// </summary>
    private async Task<List<LlmChunk>> FetchWithRetryAsync(
        MessageHistory history,
        CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var chunks = new List<LlmChunk>();
                await foreach (var chunk in _chatClient.StreamAsync(
                    history.GetAll(), _options.SystemPrompt, ct).ConfigureAwait(false))
                {
                    chunks.Add(chunk);
                }
                return chunks;
            }
            catch (LlmProviderException ex) when (RetryPolicy.IsQuotaExhausted(ex))
            {
                // Hard quota exhaustion – never retry.
                throw;
            }
            catch (LlmProviderException ex) when (RetryPolicy.IsTransient(ex) && attempt < _options.MaxRetryAttempts)
            {
                var delay = RetryPolicy.GetDelay(attempt, _options.RetryBaseDelayMs);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
    }
}
