using System.Text.Json;
using OpenClaude.Tools;
using OpenClaude.Tools.BuiltIn;

namespace OpenClaude.Core.Tests.Tools;

public sealed class FileReadToolTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"OpenClaudeTest_{Guid.NewGuid():N}");
    private readonly FileReadTool _tool = new();
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    public FileReadToolTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ToolExecutionContext Context(object input) =>
        new(_tool.Name, JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadFile_ReturnsContentWithLineNumberPrefix()
    {
        var file = Path.Combine(_tempDir, "hello.txt");
        await File.WriteAllTextAsync(file, "alpha\nbeta\ngamma");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);
        Assert.Contains("     1\talpha", result.Content);
        Assert.Contains("     2\tbeta", result.Content);
        Assert.Contains("     3\tgamma", result.Content);
    }

    [Fact]
    public async Task ReadFile_Content_EqualsOutput()
    {
        var file = Path.Combine(_tempDir, "eq.txt");
        await File.WriteAllTextAsync(file, "line");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file }));

        Assert.Equal(result.Output, result.Content);
    }

    [Fact]
    public async Task ReadFile_IsError_FalseOnSuccess()
    {
        var file = Path.Combine(_tempDir, "ok.txt");
        await File.WriteAllTextAsync(file, "ok");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file }));

        Assert.False(result.IsError);
    }

    [Fact]
    public async Task ReadFile_WithOffset_ReturnsCorrectLines()
    {
        var file = Path.Combine(_tempDir, "offset.txt");
        await File.WriteAllTextAsync(file, "one\ntwo\nthree\nfour");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file, offset = 2 }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.DoesNotContain("     1\t", result.Content);
        Assert.Contains("     3\tthree", result.Content);
        Assert.Contains("     4\tfour", result.Content);
    }

    [Fact]
    public async Task ReadFile_WithLimit_ReturnsLimitedLines()
    {
        var file = Path.Combine(_tempDir, "limit.txt");
        await File.WriteAllTextAsync(file, "a\nb\nc\nd\ne");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file, limit = 2 }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Contains("     1\ta", result.Content);
        Assert.Contains("     2\tb", result.Content);
        Assert.DoesNotContain("     3\t", result.Content);
    }

    [Fact]
    public async Task ReadFile_MetadataContainsFilePath()
    {
        var file = Path.Combine(_tempDir, "meta.txt");
        await File.WriteAllTextAsync(file, "data");

        var result = await _tool.ExecuteAsync(Context(new { file_path = file }));

        Assert.True(result.Metadata.ContainsKey("file_path"));
        Assert.Equal(file, result.Metadata["file_path"]);
    }

    [Fact]
    public async Task ReadFile_FileNotFound_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new { file_path = "/nonexistent/path/file.txt" }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task ReadFile_MissingFilePath_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new { }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        Assert.Equal("Read", _tool.Name);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
