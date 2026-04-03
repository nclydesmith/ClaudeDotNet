using System.Net.Http;

namespace DotnetClaude.Tools.BuiltIn;

public class WebFetchTool : ITool
{
    private readonly HttpClient _httpClient;
    public string Name => "WebFetch";
    public string Description => "Fetches content from a URL.";

    public WebFetchTool(HttpClient httpClient) => _httpClient = httpClient;

    public static string ConvertHtmlToMarkdown(string html) => "placeholder";

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        return ToolResult.Succeeded("URL content fetched (placeholder).");
    }
}
