using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenClaude.Tools.BuiltIn;

/// <summary>
/// Performs exact-string replacement inside a file.
/// When <c>replace_all</c> is <see langword="false"/> (the default) the tool
/// errors if <c>old_string</c> appears more than once, because an ambiguous
/// replacement would corrupt the file in a hard-to-detect way.
/// </summary>
public sealed class FileEditTool : ITool
{
    public string Name => "Edit";
    public string Description =>
        "Replaces an exact string in a file. Errors when old_string is not unique unless replace_all is true.";

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        FileEditInput input;
        try
        {
            input = JsonSerializer.Deserialize<FileEditInput>(context.InputJson,
                JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Input JSON deserialised to null.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Invalid input: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(input.FilePath))
            return ToolResult.Error("'file_path' is required.");

        if (input.OldString is null)
            return ToolResult.Error("'old_string' is required.");

        if (input.NewString is null)
            return ToolResult.Error("'new_string' is required.");

        if (!File.Exists(input.FilePath))
            return ToolResult.Error($"File not found: {input.FilePath}");

        try
        {
            var content = await File.ReadAllTextAsync(input.FilePath, cancellationToken);

            // Count occurrences so we can enforce uniqueness
            int count = CountOccurrences(content, input.OldString);

            if (count == 0)
                return ToolResult.Error($"'old_string' not found in file: {input.FilePath}");

            bool replaceAll = input.ReplaceAll ?? false;

            if (!replaceAll && count > 1)
                return ToolResult.Error(
                    $"'old_string' is not unique — found {count} occurrences. " +
                    "Set 'replace_all' to true to replace all occurrences.");

            var updated = replaceAll
                ? content.Replace(input.OldString, input.NewString, StringComparison.Ordinal)
                : ReplaceFirst(content, input.OldString, input.NewString);

            await File.WriteAllTextAsync(input.FilePath, updated, cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["file_path"] = input.FilePath,
                ["replacements"] = (replaceAll ? count : 1).ToString(CultureInfo.InvariantCulture),
            };

            return ToolResult.Succeeded(
                $"Replaced {(replaceAll ? count : 1)} occurrence(s) in {input.FilePath}",
                metadata);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Error editing file: {ex.Message}");
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static string ReplaceFirst(string text, string oldValue, string newValue)
    {
        int pos = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (pos < 0) return text;
        return string.Concat(text.AsSpan(0, pos), newValue, text.AsSpan(pos + oldValue.Length));
    }

    private sealed record FileEditInput(
        [property: JsonPropertyName("file_path")] string? FilePath,
        [property: JsonPropertyName("old_string")] string? OldString,
        [property: JsonPropertyName("new_string")] string? NewString,
        [property: JsonPropertyName("replace_all")] bool? ReplaceAll);
}
