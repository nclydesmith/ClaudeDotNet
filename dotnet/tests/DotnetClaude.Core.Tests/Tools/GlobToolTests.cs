using System.Text.Json;
using DotnetClaude.Tools;
using DotnetClaude.Tools.BuiltIn;

namespace DotnetClaude.Core.Tests.Tools;

public sealed class GlobToolTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"DotnetClaudeGlobTest_{Guid.NewGuid():N}");
    private readonly GlobTool _tool = new();
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    public GlobToolTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ToolExecutionContext Context(object input) =>
        new(_tool.Name, JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GlobTool_MatchesTsFilesRecursively()
    {
        // Arrange: create fixture files
        var subDir = Path.Combine(_tempDir, "src");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "index.ts"), "// root");
        await File.WriteAllTextAsync(Path.Combine(subDir, "helper.ts"), "// helper");
        await File.WriteAllTextAsync(Path.Combine(subDir, "util.js"), "// js");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "**/*.ts",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);
        Assert.Contains("index.ts", result.Content);
        Assert.Contains("helper.ts", result.Content);
        Assert.DoesNotContain("util.js", result.Content);
    }

    [Fact]
    public async Task GlobTool_ReturnsSortedByModificationTime()
    {
        // Create files with slightly different write times
        var older = Path.Combine(_tempDir, "older.cs");
        var newer = Path.Combine(_tempDir, "newer.cs");

        await File.WriteAllTextAsync(older, "// older");
        // Small delay to guarantee different mtime
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddSeconds(-10));

        await File.WriteAllTextAsync(newer, "// newer");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "*.cs",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        var lines = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // Most-recently-modified file should appear first
        Assert.Contains("newer.cs", lines[0]);
        Assert.Contains("older.cs", lines[1]);
    }

    [Fact]
    public async Task GlobTool_NoMatches_ReturnsEmptySuccess()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "file.txt"), "text");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "**/*.cs",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal(string.Empty, result.Content);
    }

    [Fact]
    public async Task GlobTool_MetadataContainsMatchCount()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "a.ts"), "a");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "b.ts"), "b");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "*.ts",
            path = _tempDir,
        }));

        Assert.True(result.Metadata.ContainsKey("match_count"));
        Assert.Equal("2", result.Metadata["match_count"]);
    }

    [Fact]
    public async Task GlobTool_DirectoryNotFound_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "**/*.ts",
            path = "/nonexistent/path",
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GlobTool_MissingPattern_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new { path = _tempDir }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        Assert.Equal("Glob", _tool.Name);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
