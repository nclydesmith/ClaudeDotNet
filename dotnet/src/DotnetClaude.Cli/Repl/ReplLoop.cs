using DotnetClaude.Core.Query;

namespace DotnetClaude.Cli.Repl;

/// <summary>
/// Adapter interface that decouples the REPL from <see cref="QueryEngine"/> so tests
/// can inject a stub without needing a real LLM provider.
/// </summary>
public interface IQueryAdapter
{
    /// <summary>
    /// Runs a query for the current conversation history and streams response chunks.
    /// </summary>
    IAsyncEnumerable<QueryChunk> RunAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct);
}

/// <summary>
/// The interactive read-eval-print loop.
///
/// <list type="bullet">
///   <item>Displays a prompt and reads user input via <see cref="InputReader"/>.</item>
///   <item>Dispatches to <see cref="IQueryAdapter"/> and streams response tokens.</item>
///   <item>Updates and renders <see cref="StatusLine"/> after each turn.</item>
///   <item>Handles <see cref="OperationCanceledException"/> from Ctrl+C gracefully.</item>
/// </list>
/// </summary>
public sealed class ReplLoop(
    IQueryAdapter engine,
    TerminalRenderer renderer,
    InputReader inputReader)
{
    /// <summary>
    /// Starts the REPL, blocking until the user exits or <paramref name="ct"/> is cancelled.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        var history = new List<ChatMessage>();
        var status = new StatusLine();

        while (!ct.IsCancellationRequested)
        {
            renderer.WritePrompt();

            string? userInput;
            try
            {
                userInput = await inputReader.ReadLineAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                renderer.WriteCancellationNotice();
                return;
            }

            // EOF (e.g. stdin closed / Ctrl+D)
            if (userInput is null)
                return;

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            history.Add(ChatMessage.User(userInput));

            try
            {
                await foreach (var chunk in engine.RunAsync(history, ct).ConfigureAwait(false))
                {
                    switch (chunk)
                    {
                        case TextQueryChunk text:
                            renderer.WriteToken(text.Text);
                            break;
                        case UsageQueryChunk usage:
                            status.Update(usage.PromptTokens, usage.CompletionTokens);
                            break;
                    }
                }

                renderer.EndResponse();
                renderer.WriteStatusLine(status);
            }
            catch (OperationCanceledException)
            {
                renderer.WriteCancellationNotice();
                return;
            }
            catch (Exception ex)
            {
                renderer.EndResponse();
                renderer.WriteError(ex.Message);
            }
        }
    }
}
