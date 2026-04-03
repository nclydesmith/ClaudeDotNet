using System.Text.Json;
using OpenClaude.Tools;
using OpenClaude.Tools.BuiltIn;

namespace OpenClaude.Core.Tests.Tools;

public sealed class FileEditToolTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"OpenClaudeEditTest_{Guid.NewGuid():N}");
    private readonly FileEditTool _tool = new();
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    public FileEditToolTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ToolExecutionContext Context(object input) =>
        new(_tool.Name, JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditFile_ReplacesUniqueOldString()
    {
        var file = Path.Combine(_tempDir, "edit.txt");
        await File.WriteAllTextAsync(file, "hello world");

        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = file,
            old_string = "world",
            new_string = "dotnet",
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal("hello dotnet", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task EditFile_ErrorsWhenOldStringNotUnique()
    {
        var file = Path.Combine(_tempDir, "dup.txt");
        await File.WriteAllTextAsync(file, "foo foo foo");

        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = file,
            old_string = "foo",
            new_string = "bar",
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.True(result.IsError);
        // File must be unchanged
        Assert.Equal("foo foo foo", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task EditFile_ReplaceAll_ReplacesAllOccurrences()
    {
        var file = Path.Combine(_tempDir, "all.txt");
        await File.WriteAllTextAsync(file, "cat cat cat");

        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = file,
            old_string = "cat",
            new_string = "dog",
            replace_all = true,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal("dog dog dog", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task EditFile_ErrorsWhenOldStringNotFound()
    {
        var file = Path.Combine(_tempDir, "notfound.txt");
        await File.WriteAllTextAsync(file, "hello");

        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = file,
            old_string = "xyz",
            new_string = "abc",
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task EditFile_MetadataContainsReplacementCount()
    {
        var file = Path.Combine(_tempDir, "meta.txt");
        await File.WriteAllTextAsync(file, "a-b-c");

        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = file,
            old_string = "b",
            new_string = "B",
        }));

        Assert.True(result.Metadata.ContainsKey("replacements"));
        Assert.Equal("1", result.Metadata["replacements"]);
    }

    [Fact]
    public async Task EditFile_FileNotFound_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new
        {
            file_path = "/nonexistent/file.txt",
            old_string = "x",
            new_string = "y",
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task EditFile_MissingParameters_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new { }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        Assert.Equal("Edit", _tool.Name);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
