using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenClaude.Tools.BuiltIn;

/// <summary>
/// Writes content to a file on the local filesystem, creating the file (and any
/// missing parent directories) if it does not already exist.
/// </summary>
public sealed class FileWriteTool : ITool
{
    public string Name => "Write";
    public string Description => "Writes content to a file, creating parent directories as needed.";

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        FileWriteInput input;
        try
        {
            input = JsonSerializer.Deserialize<FileWriteInput>(context.InputJson,
                JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Input JSON deserialised to null.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Invalid input: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(input.FilePath))
            return ToolResult.Error("'file_path' is required.");

        if (input.Content is null)
            return ToolResult.Error("'content' is required.");

        try
        {
            var dir = Path.GetDirectoryName(input.FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(input.FilePath, input.Content, cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["file_path"] = input.FilePath,
                ["bytes_written"] = System.Text.Encoding.UTF8.GetByteCount(input.Content).ToString(CultureInfo.InvariantCulture),
            };

            return ToolResult.Succeeded($"File written successfully: {input.FilePath}", metadata);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Error writing file: {ex.Message}");
        }
    }

    private sealed record FileWriteInput(
        [property: JsonPropertyName("file_path")] string? FilePath,
        [property: JsonPropertyName("content")] string? Content);
}
