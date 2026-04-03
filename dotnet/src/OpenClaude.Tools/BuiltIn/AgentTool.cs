namespace OpenClaude.Tools.BuiltIn;

public class AgentTool : ITool
{
    public string Name => "Agent";
    public string Description => "Spawns a sub-agent to perform tasks.";
    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        return ToolResult.Succeeded("Agent spawned (placeholder).");
    }
}
