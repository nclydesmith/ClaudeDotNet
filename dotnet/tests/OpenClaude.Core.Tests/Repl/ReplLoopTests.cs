using System.Runtime.CompilerServices;
using OpenClaude.Cli.Repl;
using OpenClaude.Core.Query;

namespace OpenClaude.Core.Tests.Repl;

// ── Stub helpers ─────────────────────────────────────────────────────────────

/// <summary>
/// Stub query adapter that yields a configurable sequence of chunks, with optional
/// per-call delays to simulate streaming (useful for cancellation tests).
/// </summary>
file sealed class StubQueryAdapter(
    IReadOnlyList<QueryChunk> chunks,
    Func<int, TimeSpan>? delayPerChunk = null) : IQueryAdapter
{
    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = [];

    public async IAsyncEnumerable<QueryChunk> RunAsync(
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ReceivedMessages.Add(messages);

        for (var i = 0; i < chunks.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            if (delayPerChunk is not null)
                await Task.Delay(delayPerChunk(i), ct).ConfigureAwait(false);

            yield return chunks[i];
        }
    }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class ReplLoopTests
{
    /// <summary>Returns an <see cref="InputReader"/> that yields the given lines then EOF.</summary>
    private static InputReader MakeInputReader(params string[] lines) =>
        new(new StringReader(string.Join(Environment.NewLine, lines)));

    /// <summary>
    /// ReplLoop displays a prompt, sends the user message to the engine,
    /// writes streamed text tokens, and shows the status line.
    /// AC: "The REPL loop starts, displays a prompt, sends a message to a stubbed QueryEngine,
    ///      and streams the response tokens to the terminal — verified by a unit test with a
    ///      captured StringWriter output."
    /// </summary>
    [Fact]
    public async Task RunAsync_StreamsTokensAndStatusLine_ToWriter()
    {
        // Arrange
        var writer = new StringWriter();
        var renderer = TerminalRenderer.CreateForWriter(writer);

        var engineChunks = new QueryChunk[]
        {
            new TextQueryChunk("Hello"),
            new TextQueryChunk(", world!"),
            new UsageQueryChunk(PromptTokens: 10, CompletionTokens: 5),
        };
        var engine = new StubQueryAdapter(engineChunks);
        var inputReader = MakeInputReader("hi");

        var repl = new ReplLoop(engine, renderer, inputReader);

        // Act
        await repl.RunAsync(CancellationToken.None);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Hello", output);
        Assert.Contains(", world!", output);
        Assert.Contains("prompt: 10", output);
        Assert.Contains("completion: 5", output);
    }

    /// <summary>
    /// The engine receives the user message as the last ChatMessage in the history.
    /// </summary>
    [Fact]
    public async Task RunAsync_PassesUserMessageToEngine()
    {
        // Arrange
        var writer = new StringWriter();
        var renderer = TerminalRenderer.CreateForWriter(writer);

        var engine = new StubQueryAdapter([new TextQueryChunk("ok")]);
        var inputReader = MakeInputReader("tell me something");
        var repl = new ReplLoop(engine, renderer, inputReader);

        // Act
        await repl.RunAsync(CancellationToken.None);

        // Assert
        var received = Assert.Single(engine.ReceivedMessages);
        var lastMsg = received[^1];
        Assert.Equal("user", lastMsg.Role);
        var textBlock = Assert.IsType<TextBlock>(lastMsg.Content.Single());
        Assert.Equal("tell me something", textBlock.Text);
    }

    /// <summary>
    /// Cancelling mid-stream prints a cancellation notice and does not throw.
    /// AC: "Pressing Ctrl+C during streaming cancels the CancellationToken passed to
    ///      QueryEngine.RunAsync() and prints a cancellation notice without throwing an
    ///      unhandled exception."
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenCancelledDuringStream_PrintsCancellationNoticeWithoutThrowing()
    {
        // Arrange
        var writer = new StringWriter();
        var renderer = TerminalRenderer.CreateForWriter(writer);

        using var cts = new CancellationTokenSource();

        // Engine yields one token then blocks long enough for us to cancel
        var engineChunks = new QueryChunk[]
        {
            new TextQueryChunk("first token"),
        };
        var engine = new StubQueryAdapter(engineChunks, delayPerChunk: _ => TimeSpan.FromSeconds(10));
        var inputReader = MakeInputReader("query");
        var repl = new ReplLoop(engine, renderer, inputReader);

        // Cancel almost immediately after starting
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act — must not throw
        var exception = await Record.ExceptionAsync(() => repl.RunAsync(cts.Token));

        // Assert
        Assert.Null(exception);
        var output = writer.ToString();
        Assert.Contains("cancelled", output, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Empty / whitespace input lines are skipped without calling the engine.
    /// </summary>
    [Fact]
    public async Task RunAsync_SkipsBlankInput()
    {
        // Arrange
        var writer = new StringWriter();
        var renderer = TerminalRenderer.CreateForWriter(writer);

        var engine = new StubQueryAdapter([]);
        // two blank lines then EOF
        var inputReader = MakeInputReader("   ", "");
        var repl = new ReplLoop(engine, renderer, inputReader);

        // Act
        await repl.RunAsync(CancellationToken.None);

        // Assert — engine was never called
        Assert.Empty(engine.ReceivedMessages);
    }

    /// <summary>
    /// Token usage is updated from UsageQueryChunk and reflected in the status line.
    /// AC: "Token usage (prompt tokens / completion tokens) is displayed in the status line
    ///      after each response."
    /// </summary>
    [Fact]
    public async Task RunAsync_DisplaysTokenUsageFromUsageQueryChunk()
    {
        // Arrange
        var writer = new StringWriter();
        var renderer = TerminalRenderer.CreateForWriter(writer);

        var engine = new StubQueryAdapter(
        [
            new TextQueryChunk("response text"),
            new UsageQueryChunk(PromptTokens: 42, CompletionTokens: 7),
        ]);
        var inputReader = MakeInputReader("question");
        var repl = new ReplLoop(engine, renderer, inputReader);

        // Act
        await repl.RunAsync(CancellationToken.None);

        // Assert
        var output = writer.ToString();
        Assert.Contains("42", output);
        Assert.Contains("7", output);
        Assert.Contains("prompt", output);
        Assert.Contains("completion", output);
    }
}
