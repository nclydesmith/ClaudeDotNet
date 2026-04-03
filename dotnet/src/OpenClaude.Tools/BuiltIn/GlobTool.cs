using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace OpenClaude.Tools.BuiltIn;

/// <summary>
/// Returns files matching a glob pattern (e.g. <c>**/*.cs</c>) under a root
/// directory, sorted by last-write time (most recently modified first).
/// </summary>
public sealed class GlobTool : ITool
{
    public string Name => "Glob";
    public string Description =>
        "Returns files matching a glob pattern in a directory, sorted by modification time.";

    public Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        GlobInput input;
        try
        {
            input = JsonSerializer.Deserialize<GlobInput>(context.InputJson,
                JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Input JSON deserialised to null.");
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Error($"Invalid input: {ex.Message}"));
        }

        if (string.IsNullOrWhiteSpace(input.Pattern))
            return Task.FromResult(ToolResult.Error("'pattern' is required."));

        var root = string.IsNullOrWhiteSpace(input.Path)
            ? Directory.GetCurrentDirectory()
            : input.Path;

        if (!Directory.Exists(root))
            return Task.FromResult(ToolResult.Error($"Directory not found: {root}"));

        try
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(input.Pattern);

            var dirInfo = new DirectoryInfoWrapper(new DirectoryInfo(root));
            var result = matcher.Execute(dirInfo);

            // Sort matches by last-write time, most-recently-modified first
            var sorted = result.Files
                .Select(f => Path.GetFullPath(Path.Combine(root, f.Path)))
                .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
                .ToList();

            var sb = new StringBuilder();
            foreach (var path in sorted)
                sb.AppendLine(path);

            var metadata = new Dictionary<string, string>
            {
                ["pattern"] = input.Pattern,
                ["root"] = root,
                ["match_count"] = sorted.Count.ToString(CultureInfo.InvariantCulture),
            };

            return Task.FromResult(ToolResult.Succeeded(sb.ToString().TrimEnd(), metadata));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Error($"Error executing glob: {ex.Message}"));
        }
    }

    private sealed record GlobInput(
        [property: JsonPropertyName("pattern")] string? Pattern,
        [property: JsonPropertyName("path")] string? Path);
}
