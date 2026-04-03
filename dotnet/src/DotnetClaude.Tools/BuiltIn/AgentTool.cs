using System.Text.Json;

namespace DotnetClaude.Tools.BuiltIn;

public interface IChildAgentRunner
{
    Task<string> RunAsync(string prompt, CancellationToken cancellationToken = default);
}

public class AgentTool : ITool
{
    private readonly IChildAgentRunner _runner;
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    
    public string Name => "Agent";
    public string Description => "Spawns a sub-agent to perform tasks.";

    public AgentTool(IChildAgentRunner runner) => _runner = runner;

    public record Input(string Prompt);

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        try 
        {
            var input = JsonSerializer.Deserialize<Input>(context.InputJson, _options);
            if (input?.Prompt == null) return ToolResult.Error("Agent execution failed: Missing prompt.");
            
            var response = await _runner.RunAsync(input.Prompt, cancellationToken);
            return ToolResult.Succeeded(response);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Agent execution failed: {ex.Message}");
        }
    }
}
