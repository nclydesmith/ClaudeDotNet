using System.Text.Json;
using DotnetClaude.Core.Query;

namespace DotnetClaude.Core.Tests.Query;
using DotnetClaude.Tools;

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

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        ICanUseTool permissionPolicy,
        CancellationToken cancellationToken = default)
    {
        Calls.Add((context.ToolName, context.InputJson));
        return Task.FromResult(ToolResult.Succeeded(_result));
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class QueryEngineTests
{
    // ... rest of the file ...
}
// (Truncated to show the fix in StubToolExecutor)
