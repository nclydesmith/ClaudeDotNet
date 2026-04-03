using System.Reflection;
using OpenClaude.Cli.Repl;
using OpenClaude.Core.Query;

// ── --version flag ────────────────────────────────────────────────────────────
if (args.Length > 0 && args[0] == "--version")
{
    var version = Assembly
        .GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0";

    Console.WriteLine(version);
    return 0;
}

// ── Wire up the REPL ──────────────────────────────────────────────────────────
using var cts = new CancellationTokenSource();

// Cancel gracefully on Ctrl+C
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // prevent immediate process kill
    cts.Cancel();
};

var renderer = TerminalRenderer.CreateDefault();
var inputReader = new InputReader(Console.In);

// Stub engine until a real provider is configured via env vars
var stubEngine = new StubQueryAdapter();
var repl = new ReplLoop(stubEngine, renderer, inputReader);

try
{
    await repl.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Already handled inside ReplLoop; nothing to do here.
}

return 0;

// ── Minimal stub adapter (placeholder until provider integration in SUB-008+) ─

/// <summary>
/// Placeholder adapter used when no real LLM provider is configured.
/// Echoes the last user message so the REPL is exercisable without credentials.
/// </summary>
file sealed class StubQueryAdapter : IQueryAdapter
{
#pragma warning disable CS1998 // async iterator with no await – intentional stub
    public async IAsyncEnumerable<QueryChunk> RunAsync(
        IReadOnlyList<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var lastUserText = messages
            .LastOrDefault(m => m.Role == "user")
            ?.Content
            .OfType<TextBlock>()
            .LastOrDefault()
            ?.Text ?? string.Empty;

        yield return new TextQueryChunk($"[stub] echo: {lastUserText}");
        yield return new UsageQueryChunk(PromptTokens: 0, CompletionTokens: 0);
    }
#pragma warning restore CS1998
}
