using System.Reflection;
using DotnetClaude.Cli.Repl;
using DotnetClaude.Core.Query;
using DotnetClaude.Core.Config;
using DotnetClaude.Providers;
using DotnetClaude.Tools;

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

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var renderer = TerminalRenderer.CreateDefault();
var inputReader = new InputReader(Console.In);

// Load settings and resolve provider
var settings = new SettingsStore("settings.json").Load();
var config = ProviderResolver.Resolve(); 

// Instantiate the provider client
var provider = new AnthropicProvider();
var client = provider.CreateStreamingClient(config);

var registry = new ToolRegistry(); 
var executor = new ToolExecutor(registry); 
var options = new QueryOptions();
var engine = new QueryEngine(client, executor, options);

// Create an adapter
var adapter = new QueryEngineAdapter(engine);

var repl = new ReplLoop(adapter, renderer, inputReader);

try
{
    await repl.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
}

return 0;

file sealed class QueryEngineAdapter(QueryEngine engine) : IQueryAdapter
{
    public IAsyncEnumerable<QueryChunk> RunAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct)
    {
        return engine.RunAsync(messages, ct);
    }
}
