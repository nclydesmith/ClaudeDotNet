using System.Runtime.CompilerServices;

namespace DotnetClaude.Core.Query;
using DotnetClaude.Tools;
using DotnetClaude.Providers;

public sealed class QueryEngine
{
    private readonly ILlmStreamingClient _chatClient;
    private readonly DotnetClaude.Tools.IToolExecutor _toolExecutor;
    private readonly QueryOptions _options;

    public QueryEngine(
        ILlmStreamingClient chatClient,
        DotnetClaude.Tools.IToolExecutor toolExecutor,
        QueryOptions options)
    {
        _chatClient = chatClient;
        _toolExecutor = toolExecutor;
        _options = options;
    }

    public async IAsyncEnumerable<QueryChunk> RunAsync(
        IReadOnlyList<ChatMessage> initialMessages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = new MessageHistory(initialMessages);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var turnChunks = await FetchWithRetryAsync(history, cancellationToken)
                .ConfigureAwait(false);

            var assistantBlocks = new List<ContentBlock>();
            var toolUses = new List<ToolUseLlmChunk>();

            foreach (var chunk in turnChunks)
            {
                if (chunk is TextDeltaLlmChunk { Delta: var delta })
                {
                    assistantBlocks.Add(new TextBlock(delta));
                    yield return new TextQueryChunk(delta);
                }
                else if (chunk is ToolUseLlmChunk toolUse)
                {
                    assistantBlocks.Add(new ToolUseBlock(toolUse.Id, toolUse.Name, toolUse.InputJson));
                    toolUses.Add(toolUse);
                    yield return new ToolUseQueryChunk(toolUse.Id, toolUse.Name, toolUse.InputJson);
                }
            }

            history.Add(ChatMessage.Assistant(assistantBlocks));

            if (toolUses.Count == 0)
                break;

            var resultBlocks = new List<ContentBlock>(toolUses.Count);
            foreach (var toolUse in toolUses)
            {
                string result;
                bool isError;
                try
                {
                    var context = new ToolExecutionContext(toolUse.Name, toolUse.InputJson, NoOpSink.Instance);
                    var policy = new ToolPermissionPolicy(); 
                    result = (await _toolExecutor.ExecuteAsync(context, policy, cancellationToken)).Content ?? string.Empty;
                    isError = false;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result = ex.Message;
                    isError = true;
                }

                resultBlocks.Add(new ToolResultBlock(toolUse.Id, result, isError));
                yield return new ToolResultQueryChunk(toolUse.Id, result, isError);
            }

            history.Add(ChatMessage.User(resultBlocks));
            history.Trim(_options.MaxContextTokens);
        }
    }

    private static async Task<List<LlmChunk>> FetchWithRetryAsync(MessageHistory history, CancellationToken ct)
    {
        // Placeholder
        return new List<LlmChunk>();
    }

    private sealed class NoOpSink : IToolProgressSink
    {
        public static readonly NoOpSink Instance = new();
        public ValueTask ReportAsync(ToolProgressEvent e, CancellationToken ct = default) => ValueTask.CompletedTask;
    }
}
