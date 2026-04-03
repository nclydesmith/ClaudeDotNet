namespace OpenClaude.Cli.Repl;

/// <summary>
/// Reads a line of user input from the given <see cref="TextReader"/>,
/// respecting the supplied <see cref="CancellationToken"/>.
/// </summary>
public sealed class InputReader(TextReader @in)
{
    /// <summary>
    /// Reads the next line of input.
    /// Returns <see langword="null"/> when the reader reaches end-of-stream (e.g. stdin is closed).
    /// Throws <see cref="OperationCanceledException"/> when <paramref name="ct"/> is cancelled.
    /// </summary>
    public async Task<string?> ReadLineAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await Task.Run(() => @in.ReadLine(), ct).ConfigureAwait(false);
    }
}
