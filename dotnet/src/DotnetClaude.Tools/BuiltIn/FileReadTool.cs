using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetClaude.Tools.BuiltIn;

/// <summary>
/// Reads a file from the local filesystem and returns its content with
/// 1-based line-number prefixes (matching the Claude Code Read tool contract).
/// </summary>
public sealed class FileReadTool : ITool
{
    public string Name => "Read";
    public string Description => "Reads a file from the local filesystem and returns its contents with line-number prefixes.";

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        FileReadInput input;
        try
        {
            input = JsonSerializer.Deserialize<FileReadInput>(context.InputJson,
                JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Input JSON deserialised to null.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Invalid input: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(input.FilePath))
            return ToolResult.Error("'file_path' is required.");

        if (!File.Exists(input.FilePath))
            return ToolResult.Error($"File not found: {input.FilePath}");

        try
        {
            var lines = await File.ReadAllLinesAsync(input.FilePath, cancellationToken);

            int offset = Math.Max(0, input.Offset ?? 0);
            int limit = input.Limit ?? lines.Length;

            var sb = new StringBuilder();
            int end = Math.Min(offset + limit, lines.Length);
            for (int i = offset; i < end; i++)
            {
                // Format: right-aligned 6-digit line number + tab
                sb.Append(CultureInfo.InvariantCulture, $"{i + 1,6}\t{lines[i]}\n");
            }

            var metadata = new Dictionary<string, string>
            {
                ["file_path"] = input.FilePath,
                ["total_lines"] = lines.Length.ToString(CultureInfo.InvariantCulture),
                ["lines_returned"] = (end - offset).ToString(CultureInfo.InvariantCulture),
            };

            return ToolResult.Succeeded(sb.ToString(), metadata);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Error reading file: {ex.Message}");
        }
    }

    private sealed record FileReadInput(
        [property: JsonPropertyName("file_path")] string? FilePath,
        [property: JsonPropertyName("offset")] int? Offset,
        [property: JsonPropertyName("limit")] int? Limit);
}
