using System.Text.Json;
using OpenClaude.Tools;
using OpenClaude.Tools.BuiltIn;

namespace OpenClaude.Core.Tests.Tools;

public sealed class GrepToolTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"OpenClaudeGrepTest_{Guid.NewGuid():N}");
    private readonly GrepTool _tool = new();
    private readonly IToolProgressSink _sink = NoOpSink.Instance;

    public GrepToolTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ToolExecutionContext Context(object input) =>
        new(_tool.Name, JsonSerializer.Serialize(input, JsonSerializerOptions.Web), _sink);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Grep_ReturnsMatchingLinesWithFileAndLineNumber()
    {
        var file = Path.Combine(_tempDir, "sample.txt");
        await File.WriteAllTextAsync(file, "alpha\nbeta\nalpha beta\ngamma");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "alpha",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.NotNull(result.Content);

        var lines = result.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        // Each line should be in filepath:linenum:content format
        Assert.All(lines, l => Assert.Contains("sample.txt", l));
        Assert.Contains(":1:", lines[0]);  // line 1
        Assert.Contains(":3:", lines[1]);  // line 3
    }

    [Fact]
    public async Task Grep_SupportsRegexPatterns()
    {
        var file = Path.Combine(_tempDir, "regex.txt");
        await File.WriteAllTextAsync(file, "foo123\nbar456\nfoo789");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = @"foo\d+",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        var lines = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.All(lines, l => Assert.Contains("foo", l));
    }

    [Fact]
    public async Task Grep_SearchesAcrossMultipleFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "a.txt"), "hello from a");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "b.txt"), "hello from b");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "c.txt"), "nothing here");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "hello",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        var matches = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, matches.Length);
        Assert.Contains(matches, l => l.Contains("a.txt"));
        Assert.Contains(matches, l => l.Contains("b.txt"));
    }

    [Fact]
    public async Task Grep_WithGlobFilter_OnlySearchesMatchingFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "code.cs"), "var x = 1;");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "notes.txt"), "var note = true;");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "var",
            path = _tempDir,
            glob = "*.cs",
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        var lines = result.Content!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.Contains("code.cs", lines[0]);
    }

    [Fact]
    public async Task Grep_NoMatches_ReturnsEmptySuccess()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "empty.txt"), "nothing");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "xyz_not_present",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Success, result.Status);
        Assert.Equal(string.Empty, result.Content);
    }

    [Fact]
    public async Task Grep_MetadataContainsMatchCount()
    {
        var file = Path.Combine(_tempDir, "count.txt");
        await File.WriteAllTextAsync(file, "hit\nmiss\nhit");

        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "hit",
            path = _tempDir,
        }));

        Assert.True(result.Metadata.ContainsKey("match_count"));
        Assert.Equal("2", result.Metadata["match_count"]);
    }

    [Fact]
    public async Task Grep_InvalidRegex_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "[invalid",
            path = _tempDir,
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Grep_PathNotFound_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new
        {
            pattern = "hello",
            path = "/nonexistent/path",
        }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Grep_MissingPattern_ReturnsError()
    {
        var result = await _tool.ExecuteAsync(Context(new { path = _tempDir }));

        Assert.Equal(ToolResultStatus.Error, result.Status);
    }

    [Fact]
    public void Tool_HasCorrectName()
    {
        Assert.Equal("Grep", _tool.Name);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
