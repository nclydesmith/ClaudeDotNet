using DotnetClaude.Cli.Commands;
using Xunit;

namespace DotnetClaude.Core.Tests.Commands;

public class CommandRouterTests
{
    [Fact]
    public void Register_ValidCommand_Succeeds()
    {
        // Arrange
        var router = new CommandRouter();
        var command = new MockCommand("/test", "Test command");

        // Act
        router.Register(command);

        // Assert
        Assert.Equal(command, router.Resolve("/test"));
    }

    [Fact]
    public void Resolve_CaseInsensitive_Succeeds()
    {
        // Arrange
        var router = new CommandRouter();
        var command = new MockCommand("/TEST", "Test command");
        router.Register(command);

        // Act & Assert
        Assert.Equal(command, router.Resolve("/test"));
    }

    [Fact]
    public async Task ExecuteAsync_ValidCommand_CallsExecute()
    {
        // Arrange
        var router = new CommandRouter();
        var command = new MockCommand("/test", "Test command");
        router.Register(command);

        // Act
        await router.ExecuteAsync("/test arg1 arg2", CancellationToken.None);

        // Assert
        Assert.True(command.WasExecuted);
        Assert.Equal("arg1 arg2", command.LastArgs);
    }

    private sealed class MockCommand(string name, string description) : ISlashCommand
    {
        public string Name => name;
        public string Description => description;
        public bool WasExecuted { get; private set; }
        public string? LastArgs { get; private set; }

        public Task ExecuteAsync(string args, CancellationToken cancellationToken)
        {
            WasExecuted = true;
            LastArgs = args;
            return Task.CompletedTask;
        }
    }
}
