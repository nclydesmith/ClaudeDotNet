using System.Collections.Concurrent;

namespace DotnetClaude.Cli.Commands;

/// <summary>
/// Registers slash commands and routes user input to the correct handler.
/// </summary>
public class CommandRouter
{
    private readonly ConcurrentDictionary<string, ISlashCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ISlashCommand command)
    {
        if (!_commands.TryAdd(command.Name, command))
        {
            throw new ArgumentException($"Command already registered: {command.Name}", nameof(command));
        }
    }

    public ISlashCommand? Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/'))
        {
            return null;
        }

        string commandName = input.Split(' ')[0];
        return _commands.TryGetValue(commandName, out var command) ? command : null;
    }

    public IEnumerable<ISlashCommand> GetAll() => _commands.Values.OrderBy(c => c.Name);

    public async Task ExecuteAsync(string input, CancellationToken cancellationToken)
    {
        var command = Resolve(input);
        if (command == null)
        {
            Console.WriteLine($"Unknown command: {input.Split(' ')[0]}. Type /help for a list of commands.");
            return;
        }

        string args = input.Contains(' ') ? input.Substring(input.IndexOf(' ') + 1) : string.Empty;
        await command.ExecuteAsync(args, cancellationToken);
    }
}
