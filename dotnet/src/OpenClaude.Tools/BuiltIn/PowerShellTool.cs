using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenClaude.Tools.BuiltIn;

public class PowerShellTool : ITool
{
    private const int DefaultTimeoutMs = 120_000;

    public string Name => "PowerShell";
    public string Description => "Execute shell commands in PowerShell (Windows/macOS/Linux).";

    public record Input(
        [property: JsonPropertyName("command")] string Command,
        [property: JsonPropertyName("timeout")] int? TimeoutMs = null);

    public async Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)
    {
        var input = JsonSerializer.Deserialize<Input>(context.InputJson);
        if (input == null || string.IsNullOrWhiteSpace(input.Command))
        {
            return ToolResult.Error("No command provided.");
        }

        string shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "pwsh";
        string args = $"-Command \"{input.Command.Replace("\"", "\\\"")}\"";

        var timeout = TimeSpan.FromMilliseconds(input.TimeoutMs ?? DefaultTimeoutMs);

        var result = await ProcessRunner.RunAsync(
            Name,
            shell,
            args,
            timeout,
            cancellationToken,
            context.ProgressSink
        );

        return result.Interrupted 
            ? ToolResult.Error(result.Stderr) 
            : ToolResult.Succeeded(result.Stdout + result.Stderr);
    }
}
