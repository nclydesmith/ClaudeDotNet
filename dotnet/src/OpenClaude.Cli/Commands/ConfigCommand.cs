using Spectre.Console;

namespace OpenClaude.Cli.Commands;

public class ConfigCommand : ISlashCommand
{
    public string Name => "/config";
    public string Description => "Show or edit application configuration.";

    public Task ExecuteAsync(string args, CancellationToken cancellationToken)
    {
        if (args.Trim().Equals("show", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(args))
        {
            AnsiConsole.MarkupLine("[bold blue]Current Configuration:[/]");
            AnsiConsole.MarkupLine("[grey]- .openclaude/settings.json[/]");
            AnsiConsole.MarkupLine("[grey]- Environment variables[/]");
            // Note: Full implementation deferred to SUB-009 (Settings management)
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Unknown config subcommand. Try '/config show'.[/]");
        }

        return Task.CompletedTask;
    }
}
