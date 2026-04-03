namespace OpenClaude.Tools.BuiltIn;

/// <summary>A single web search result returned by a search provider.</summary>
public sealed record WebSearchResult(string Title, string Url, string Snippet);

/// <summary>
/// Abstraction over a web search backend (e.g. Brave Search, SerpAPI).
/// Inject a fake implementation in tests; wire the real provider in production.
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>
    /// Executes a web search and returns a ranked list of results.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>Ordered list of search results (may be empty if no results found).</returns>
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default);
}
