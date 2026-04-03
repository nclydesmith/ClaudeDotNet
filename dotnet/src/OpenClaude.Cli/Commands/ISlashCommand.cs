namespace OpenClaude.Cli.Commands;

/// <summary>
/// Represents a local slash command (e.g., /help, /config) that can be executed from the REPL.
/// </summary>
public interface ISlashCommand
{
    /// <summary>The name of the command, including the leading slash (e.g., "/help").</summary>
    string Name { get; }

    /// <summary>A short description of the command for the help listing.</summary>
    string Description { get; }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">The raw argument string following the command name.</param>
    /// <param name="cancellationToken">Token to cancel the execution.</param>
    Task ExecuteAsync(string args, CancellationToken cancellationToken);
}
