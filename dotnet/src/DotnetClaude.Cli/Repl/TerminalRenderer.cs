using Spectre.Console;

namespace DotnetClaude.Cli.Repl;

/// <summary>
/// Wraps <see cref="IAnsiConsole"/> to provide high-level rendering helpers for the REPL.
/// All output is routed through the injected console so tests can capture it via
/// <see cref="AnsiConsole.Create"/> with a <see cref="StringWriter"/> backing.
/// </summary>
public sealed class TerminalRenderer(IAnsiConsole console)
{
    /// <summary>Creates a renderer that writes to the real interactive terminal.</summary>
    public static TerminalRenderer CreateDefault() => new(AnsiConsole.Console);

    /// <summary>Creates a renderer backed by an arbitrary <see cref="TextWriter"/> (for tests).</summary>
    public static TerminalRenderer CreateForWriter(TextWriter writer) =>
        new(AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        }));

    /// <summary>Writes the user-input prompt.</summary>
    public void WritePrompt() => console.Markup("[bold green]>[/] ");

    /// <summary>Streams a single text token directly to the console.</summary>
    public void WriteToken(string text) => console.Write(text);

    /// <summary>Moves to a new line after a streaming response completes.</summary>
    public void EndResponse() => console.WriteLine();

    /// <summary>Renders token-usage info as a muted status line.</summary>
    public void WriteStatusLine(StatusLine status) =>
        console.MarkupLine($"[dim]{Markup.Escape(status.Format())}[/]");

    /// <summary>Renders an error inside a bordered panel.</summary>
    public void WriteError(string message) =>
        console.Write(new Panel(Markup.Escape(message))
        {
            Header = new PanelHeader("Error"),
            Border = BoxBorder.Rounded,
        });

    /// <summary>Prints a notice that the current operation was cancelled by the user.</summary>
    public void WriteCancellationNotice() =>
        console.MarkupLine("[yellow]^C — cancelled[/]");
}
