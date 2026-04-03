using System.Diagnostics;
using System.Text;

namespace OpenClaude.Tools.BuiltIn;

/// <summary>
/// A utility class for running external processes with support for timeouts and output streaming.
/// </summary>
public class ProcessRunner
{
    public record ProcessResult(int ExitCode, string Stdout, string Stderr, bool Interrupted);

    public static async Task<ProcessResult> RunAsync(
        string toolName,
        string fileName,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        IToolProgressSink? progressSink = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                stdout.AppendLine(e.Data);
                if (progressSink != null)
                {
                    // Use Task.Run to fire and forget without violating CA2012
                    _ = Task.Run(async () => await progressSink.ReportAsync(new ToolProgressMessage(toolName, e.Data), cancellationToken), cancellationToken);
                }
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                stderr.AppendLine(e.Data);
                if (progressSink != null)
                {
                    _ = Task.Run(async () => await progressSink.ReportAsync(new ToolProgressMessage(toolName, $"Error: {e.Data}"), cancellationToken), cancellationToken);
                }
            }
        };

        if (!process.Start())
        {
            return new ProcessResult(-1, "", "Failed to start process.", false);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            await process.WaitForExitAsync(timeoutCts.Token);
            
            return new ProcessResult(
                process.ExitCode,
                stdout.ToString(),
                stderr.ToString(),
                false
            );
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            bool isTimeout = !cancellationToken.IsCancellationRequested;
            return new ProcessResult(
                -1,
                stdout.ToString(),
                stderr.ToString() + (isTimeout ? "\nCommand timed out." : "\nCommand cancelled."),
                true
            );
        }
    }
}
