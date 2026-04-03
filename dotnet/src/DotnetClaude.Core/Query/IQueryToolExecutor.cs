namespace DotnetClaude.Core.Query;
using DotnetClaude.Tools;

public interface IToolExecutor
{
    Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        ICanUseTool permissionPolicy,
        CancellationToken cancellationToken = default);
}
