using Spectre.Console;

namespace DotnetClaude.Cli.Commands;

public class HelpCommand(CommandRouter router) : ISlashCommand
{
    public string Name => "/help";
    public string Description => "List all available slash commands.";

    public Task ExecuteAsync(string args, CancellationToken cancellationToken)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[blue]Command[/]")
            .AddColumn("[green]Description[/]");

        foreach (var cmd in router.GetAll())
        {
            table.AddRow($"[bold]{cmd.Name}[/]", cmd.Description);
        }

        AnsiConsole.Write(new Panel(table)
            .Header("[bold]Available Commands[/]")
            .Padding(1, 1, 1, 1));

        return Task.CompletedTask;
    }
}
