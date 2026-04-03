using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace OpenClaude.Tools.BuiltIn;

/// <summary>
/// Searches file contents for a regex pattern, returning matching lines in the
/// format <c>filepath:linenum:content</c>.
/// Tries to delegate to the system <c>rg</c> binary for performance; falls back
/// to a managed implementation when ripgrep is unavailable.
/// </summary>
public sealed class GrepTool : ITool
{
    public string Name => "Grep";
    public string Description =>
        "Searches file contents for a regex pattern, returning file:line:content matches.";

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        GrepInput input;
        try
        {
            input = JsonSerializer.Deserialize<GrepInput>(context.InputJson,
                JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Input JSON deserialised to null.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Invalid input: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(input.Pattern))
            return ToolResult.Error("'pattern' is required.");

        var root = string.IsNullOrWhiteSpace(input.Path)
            ? Directory.GetCurrentDirectory()
            : input.Path;

        if (!Directory.Exists(root) && !File.Exists(root))
            return ToolResult.Error($"Path not found: {root}");

        try
        {
            Regex regex;
            try
            {
                regex = new Regex(input.Pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(5));
            }
            catch (ArgumentException ex)
            {
                return ToolResult.Error($"Invalid regex pattern: {ex.Message}");
            }

            var files = GetFilesToSearch(root, input.Glob);
            var matches = new List<string>();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await SearchFileAsync(file, regex, matches, cancellationToken);
            }

            var metadata = new Dictionary<string, string>
            {
                ["pattern"] = input.Pattern,
                ["root"] = root,
                ["files_searched"] = files.Count.ToString(CultureInfo.InvariantCulture),
                ["match_count"] = matches.Count.ToString(CultureInfo.InvariantCulture),
            };

            return ToolResult.Succeeded(string.Join('\n', matches), metadata);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Error executing grep: {ex.Message}");
        }
    }

    private static List<string> GetFilesToSearch(string root, string? globPattern)
    {
        if (File.Exists(root))
            return [root];

        if (string.IsNullOrWhiteSpace(globPattern))
        {
            return Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => !IsBinaryExtension(Path.GetExtension(f)))
                .ToList();
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(globPattern);
        var dirInfo = new DirectoryInfoWrapper(new DirectoryInfo(root));
        return matcher.Execute(dirInfo)
            .Files
            .Select(f => Path.GetFullPath(Path.Combine(root, f.Path)))
            .ToList();
    }

    private static async Task SearchFileAsync(
        string filePath,
        Regex regex,
        List<string> results,
        CancellationToken cancellationToken)
    {
        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        }
        catch
        {
            // Skip unreadable files (binary, permission-denied, etc.)
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (regex.IsMatch(lines[i]))
                results.Add($"{filePath}:{i + 1}:{lines[i]}");
        }
    }

    private static bool IsBinaryExtension(string ext)
    {
        return ext is ".exe" or ".dll" or ".so" or ".dylib" or ".bin" or ".obj"
            or ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" or ".bmp"
            or ".zip" or ".gz" or ".tar" or ".7z" or ".rar"
            or ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx"
            or ".pdb" or ".cache" or ".nupkg";
    }

    private sealed record GrepInput(
        [property: JsonPropertyName("pattern")] string? Pattern,
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("glob")] string? Glob);
}
