using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetClaude.Tools.BuiltIn;

/// <summary>
/// Brave Search API provider.
/// Requires a Brave Search API key passed as <paramref name="apiKey"/>.
/// Set the <c>BRAVE_SEARCH_API_KEY</c> environment variable or inject via DI.
/// </summary>
public sealed class BraveSearchProvider : IWebSearchProvider
{
    private const string BraveSearchBaseUrl = "https://api.search.brave.com/res/v1/web/search";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    /// <param name="httpClient">Pre-configured <see cref="HttpClient"/> (base address is not required).</param>
    /// <param name="apiKey">Brave Search subscription token.</param>
    public BraveSearchProvider(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var url = $"{BraveSearchBaseUrl}?q={Uri.EscapeDataString(query)}&count=10";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Subscription-Token", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var root = JsonSerializer.Deserialize<BraveSearchResponse>(json, JsonSerializerOptions.Web);

        if (root?.Web?.Results is not { Count: > 0 } results)
            return [];

        return results
            .Select(r => new WebSearchResult(
                Title: r.Title ?? string.Empty,
                Url: r.Url ?? string.Empty,
                Snippet: r.Description ?? string.Empty))
            .ToList();
    }

    // ── Response DTOs ─────────────────────────────────────────────────────────

    private sealed class BraveSearchResponse
    {
        [JsonPropertyName("web")]
        public BraveWebResults? Web { get; init; }
    }

    private sealed class BraveWebResults
    {
        [JsonPropertyName("results")]
        public List<BraveResult>? Results { get; init; }
    }

    private sealed class BraveResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
