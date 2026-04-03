namespace DotnetClaude.Tools.BuiltIn;

public class WebSearchTool : ITool
{
    private readonly IWebSearchProvider _provider;
    public string Name => "WebSearch";
    public string Description => "Searches the web.";

    public WebSearchTool(IWebSearchProvider provider) => _provider = provider;

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        return ToolResult.Succeeded("Search results returned (placeholder).");
    }
}
