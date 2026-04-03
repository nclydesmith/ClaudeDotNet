using OpenClaude.Core.Query;

namespace OpenClaude.Core.Tests.Query;

// ── Stub helpers ─────────────────────────────────────────────────────────────

/// <summary>
/// Stub chat client that returns a predetermined sequence of <see cref="LlmChunk"/> values.
/// Supports an optional list of exceptions to throw on the first N calls before succeeding.
/// </summary>
file sealed class StubChatClient : ILlmChatClient
{
    private readonly IReadOnlyList<LlmChunk> _chunks;
    private readonly Queue<Exception> _errorsToThrow;
    public int CallCount { get; private set; }
    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = [];

    public StubChatClient(IReadOnlyList<LlmChunk> chunks, IEnumerable<Exception>? errorsToThrow = null)
    {
        _chunks = chunks;
        _errorsToThrow = new Queue<Exception>(errorsToThrow ?? []);
    }

#pragma warning disable CS1998 // async with no await – intentional for test stub
    public async IAsyncEnumerable<LlmChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string? systemPrompt = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallCount++;
        ReceivedMessages.Add(messages);

        if (_errorsToThrow.Count > 0)
            throw _errorsToThrow.Dequeue();

        foreach (var chunk in _chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }
#pragma warning restore CS1998
}

/// <summary>Stub tool executor that records calls and returns a fixed result.</summary>
file sealed class StubToolExecutor : IToolExecutor
{
    private readonly string _result;
    public List<(string Name, string Input)> Calls { get; } = [];

    public StubToolExecutor(string result = "tool-output") => _result = result;

    public Task<string> ExecuteAsync(string toolName, string inputJson, CancellationToken cancellationToken = default)
    {
        Calls.Add((toolName, inputJson));
        return Task.FromResult(_result);
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class QueryEngineTests
{
    // ── AC1: RunAsync yields text tokens ─────────────────────────────────────

    [Fact]
    public async Task RunAsync_SimpleTextResponse_YieldsTextChunks()
    {
        // Arrange
        var chunks = new LlmChunk[]
        {
            new TextDeltaLlmChunk("Hello"),
            new TextDeltaLlmChunk(", "),
            new TextDeltaLlmChunk("world!"),
        };
        var client = new StubChatClient(chunks);
        var engine = new QueryEngine(client, new StubToolExecutor(), new QueryOptions());

        // Act
        var results = new List<QueryChunk>();
        await foreach (var chunk in engine.RunAsync([ChatMessage.User("hi")]))
            results.Add(chunk);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("Hello", ((TextQueryChunk)results[0]).Text);
        Assert.Equal(", ", ((TextQueryChunk)results[1]).Text);
        Assert.Equal("world!", ((TextQueryChunk)results[2]).Text);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task RunAsync_EmptyResponse_YieldsNoChunks()
    {
        var client = new StubChatClient([]);
        var engine = new QueryEngine(client, new StubToolExecutor(), new QueryOptions());

        var results = new List<QueryChunk>();
        await foreach (var chunk in engine.RunAsync([ChatMessage.User("hi")]))
            results.Add(chunk);

        Assert.Empty(results);
        Assert.Equal(1, client.CallCount);
    }

    // ── AC2: Tool-use block triggers executor and follow-up request ──────────

    [Fact]
    public async Task RunAsync_ToolUseBlock_CallsExecutorAppendsResultAndContinues()
    {
        // First response: tool use block; second response: text (end of loop).
        const string toolId = "call_abc";
        const string toolName = "get_weather";
        const string toolInput = "{\"city\":\"London\"}";
        const string toolOutput = "Sunny, 20°C";

        // The client returns tool-use on first call, text on second call.
        var firstResponse = new LlmChunk[] { new ToolUseLlmChunk(toolId, toolName, toolInput) };
        var secondResponse = new LlmChunk[] { new TextDeltaLlmChunk("It is sunny.") };

        var responses = new[] { firstResponse, secondResponse };

        // Build a two-phase stub by queuing responses.
        var toolExecutor = new StubToolExecutor(toolOutput);

        // Use a specialized two-phase client.
        var twoPhaseClient = new TwoPhaseChatClient(
            responses.Select(r => (IReadOnlyList<LlmChunk>)r).ToList());

        var engine = new QueryEngine(twoPhaseClient, toolExecutor, new QueryOptions());

        // Act
        var results = new List<QueryChunk>();
        await foreach (var chunk in engine.RunAsync([ChatMessage.User("weather?")]))
            results.Add(chunk);

        // Assert: tool use + tool result + final text
        Assert.Equal(3, results.Count);
        var toolUseChunk = Assert.IsType<ToolUseQueryChunk>(results[0]);
        Assert.Equal(toolId, toolUseChunk.Id);
        Assert.Equal(toolName, toolUseChunk.Name);

        var toolResultChunk = Assert.IsType<ToolResultQueryChunk>(results[1]);
        Assert.Equal(toolId, toolResultChunk.ToolUseId);
        Assert.Equal(toolOutput, toolResultChunk.Result);
        Assert.False(toolResultChunk.IsError);

        var textChunk = Assert.IsType<TextQueryChunk>(results[2]);
        Assert.Equal("It is sunny.", textChunk.Text);

        // Executor was called once.
        Assert.Single(toolExecutor.Calls);
        Assert.Equal(toolName, toolExecutor.Calls[0].Name);
        Assert.Equal(toolInput, toolExecutor.Calls[0].Input);

        // Provider received two requests.
        Assert.Equal(2, twoPhaseClient.CallCount);

        // Second request contains the tool result in history.
        var secondRequestMessages = twoPhaseClient.ReceivedMessages[1];
        var toolResultMsg = secondRequestMessages[^1]; // last message
        Assert.Equal("user", toolResultMsg.Role);
        var resultBlock = Assert.IsType<ToolResultBlock>(toolResultMsg.Content[0]);
        Assert.Equal(toolId, resultBlock.ToolUseId);
        Assert.Equal(toolOutput, resultBlock.Content);
    }

    // ── AC3: Retry logic ─────────────────────────────────────────────────────

    [Fact]
    public void RetryPolicy_Transient429_ShouldRetry()
    {
        var ex = new LlmProviderException(429, "Rate limit exceeded");
        Assert.True(RetryPolicy.IsTransient(ex));
        Assert.False(RetryPolicy.IsQuotaExhausted(ex));
    }

    [Fact]
    public void RetryPolicy_Transient503_ShouldRetry()
    {
        var ex = new LlmProviderException(503, "Service unavailable");
        Assert.True(RetryPolicy.IsTransient(ex));
    }

    [Fact]
    public void RetryPolicy_QuotaExhausted_LimitZero_ShouldNotRetry()
    {
        var ex = new LlmProviderException(429, "You have exceeded limit: 0 tokens");
        Assert.True(RetryPolicy.IsQuotaExhausted(ex));
        Assert.False(RetryPolicy.IsTransient(ex));
    }

    [Fact]
    public void RetryPolicy_QuotaExhausted_ExceededCurrentQuota_ShouldNotRetry()
    {
        var ex = new LlmProviderException(429, "You have exceeded your current quota, please check your plan.");
        Assert.True(RetryPolicy.IsQuotaExhausted(ex));
        Assert.False(RetryPolicy.IsTransient(ex));
    }

    [Fact]
    public void RetryPolicy_NonLlmException_NotTransient()
    {
        var ex = new InvalidOperationException("something else");
        Assert.False(RetryPolicy.IsTransient(ex));
        Assert.False(RetryPolicy.IsQuotaExhausted(ex));
    }

    [Fact]
    public void RetryPolicy_GetDelay_ExponentialBackoff()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(1_000), RetryPolicy.GetDelay(0, 1_000));
        Assert.Equal(TimeSpan.FromMilliseconds(2_000), RetryPolicy.GetDelay(1, 1_000));
        Assert.Equal(TimeSpan.FromMilliseconds(4_000), RetryPolicy.GetDelay(2, 1_000));
    }

    [Fact]
    public void RetryPolicy_GetDelay_CapsAt30Seconds()
    {
        var delay = RetryPolicy.GetDelay(100, 1_000);
        Assert.Equal(TimeSpan.FromSeconds(30), delay);
    }

    [Fact]
    public async Task RunAsync_TransientError_RetriesAndSucceeds()
    {
        // First two calls throw 503; third succeeds.
        var transientError = new LlmProviderException(503, "Service unavailable");
        var successChunks = new LlmChunk[] { new TextDeltaLlmChunk("done") };

        var client = new StubChatClient(
            successChunks,
            errorsToThrow: [transientError, transientError]);

        var options = new QueryOptions { MaxRetryAttempts = 3, RetryBaseDelayMs = 0 };
        var engine = new QueryEngine(client, new StubToolExecutor(), options);

        var results = new List<QueryChunk>();
        await foreach (var chunk in engine.RunAsync([ChatMessage.User("hi")]))
            results.Add(chunk);

        Assert.Single(results);
        Assert.Equal("done", ((TextQueryChunk)results[0]).Text);
        Assert.Equal(3, client.CallCount); // 2 failures + 1 success
    }

    [Fact]
    public async Task RunAsync_QuotaExhausted_ThrowsWithoutRetry()
    {
        var quotaError = new LlmProviderException(429, "You have exceeded your current quota");
        var client = new StubChatClient([], errorsToThrow: [quotaError]);
        var options = new QueryOptions { MaxRetryAttempts = 3, RetryBaseDelayMs = 0 };
        var engine = new QueryEngine(client, new StubToolExecutor(), options);

        var ex = await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in engine.RunAsync([ChatMessage.User("hi")])) { }
        });

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(1, client.CallCount); // no retry
    }

    [Fact]
    public async Task RunAsync_ExceedsMaxRetries_Throws()
    {
        var transientError = new LlmProviderException(503, "Service unavailable");
        // Throw more times than allowed retries.
        var client = new StubChatClient(
            [],
            errorsToThrow: [transientError, transientError, transientError, transientError]);

        var options = new QueryOptions { MaxRetryAttempts = 2, RetryBaseDelayMs = 0 };
        var engine = new QueryEngine(client, new StubToolExecutor(), options);

        await Assert.ThrowsAsync<LlmProviderException>(async () =>
        {
            await foreach (var _ in engine.RunAsync([ChatMessage.User("hi")])) { }
        });

        Assert.Equal(3, client.CallCount); // maxRetryAttempts=2 means 3 total (0,1,2)
    }

    // ── AC4: MessageHistory trimming ─────────────────────────────────────────

    [Fact]
    public void MessageHistory_Trim_RemovesOldestMessagesWhenOverLimit()
    {
        var history = new MessageHistory();
        // ~400 chars each → ~100 tokens each
        var bigText = new string('a', 400);
        history.Add(ChatMessage.User(bigText)); // msg 1
        history.Add(ChatMessage.User(bigText)); // msg 2
        history.Add(ChatMessage.User(bigText)); // msg 3

        Assert.Equal(300, history.EstimateTokens()); // 1200 chars / 4

        // Trim to 150 tokens – should remove msg 1 and msg 2.
        history.Trim(maxTokens: 150);

        Assert.Equal(1, history.Count);
        Assert.Equal(100, history.EstimateTokens());
    }

    [Fact]
    public void MessageHistory_Trim_KeepsAtLeastOneMessage()
    {
        var history = new MessageHistory();
        history.Add(ChatMessage.User(new string('x', 4_000))); // ~1000 tokens

        history.Trim(maxTokens: 1); // impossible to satisfy

        Assert.Equal(1, history.Count); // never empties
    }

    [Fact]
    public void MessageHistory_Trim_NoopWhenUnderLimit()
    {
        var history = new MessageHistory();
        history.Add(ChatMessage.User("hi")); // 2 chars → 0 tokens (< 4)

        history.Trim(maxTokens: 100_000);

        Assert.Equal(1, history.Count);
    }
}

// ── Two-phase stub client (private to test file) ─────────────────────────────

/// <summary>
/// Chat client that cycles through a list of response sets, one per call.
/// Used to simulate multi-turn tool-use interactions.
/// </summary>
file sealed class TwoPhaseChatClient : ILlmChatClient
{
    private readonly IReadOnlyList<IReadOnlyList<LlmChunk>> _phases;
    private int _phase;
    public int CallCount { get; private set; }
    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = [];

    public TwoPhaseChatClient(IReadOnlyList<IReadOnlyList<LlmChunk>> phases) => _phases = phases;

#pragma warning disable CS1998
    public async IAsyncEnumerable<LlmChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string? systemPrompt = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallCount++;
        ReceivedMessages.Add(messages);
        var currentPhase = _phases[_phase++];
        foreach (var chunk in currentPhase)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }
#pragma warning restore CS1998
}
