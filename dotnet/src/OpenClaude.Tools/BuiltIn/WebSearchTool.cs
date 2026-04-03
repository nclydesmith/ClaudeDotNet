namespace OpenClaude.Tools.BuiltIn;

public class WebSearchTool : ITool
{
    public string Name => "WebSearch";
    public string Description => "Searches the web.";
    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        return ToolResult.Succeeded("Search results returned (placeholder).");
    }
}
