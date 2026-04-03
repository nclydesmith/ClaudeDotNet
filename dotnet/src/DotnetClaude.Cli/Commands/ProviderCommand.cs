using Spectre.Console;

namespace DotnetClaude.Cli.Commands;

public class ProviderCommand : ISlashCommand
{
    public string Name => "/provider";
    public string Description => "List and switch active provider profile.";

    public Task ExecuteAsync(string args, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold blue]Available Providers:[/]");
        AnsiConsole.MarkupLine("- [green]anthropic[/] (Active)");
        AnsiConsole.MarkupLine("- [grey]openai[/]");
        AnsiConsole.MarkupLine("- [grey]gemini[/]");
        AnsiConsole.MarkupLine("- [grey]ollama[/]");
        
        // Note: Full implementation deferred to SUB-009 (Settings management)
        return Task.CompletedTask;
    }
}
