namespace OpenClaude.Tools.BuiltIn;

public class WebFetchTool : ITool
{
    public string Name => "WebFetch";
    public string Description => "Fetches content from a URL.";
    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        return ToolResult.Succeeded("URL content fetched (placeholder).");
    }
}
