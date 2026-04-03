namespace OpenClaude.Core.Query;

// ── Content blocks ──────────────────────────────────────────────────────────

/// <summary>Discriminated union base for all message content block types.</summary>
public abstract record ContentBlock;

/// <summary>Plain text content.</summary>
public sealed record TextBlock(string Text) : ContentBlock;

/// <summary>A tool invocation requested by the assistant.</summary>
public sealed record ToolUseBlock(string Id, string Name, string InputJson) : ContentBlock;

/// <summary>A tool result supplied back to the assistant.</summary>
public sealed record ToolResultBlock(string ToolUseId, string Content, bool IsError = false) : ContentBlock;

// ── Chat messages ────────────────────────────────────────────────────────────

/// <summary>A single conversation turn (user or assistant).</summary>
public sealed record ChatMessage(string Role, IReadOnlyList<ContentBlock> Content)
{
    /// <summary>Creates a user message containing a single text block.</summary>
    public static ChatMessage User(string text) =>
        new("user", [new TextBlock(text)]);

    /// <summary>Creates a user message with arbitrary content blocks (e.g. tool results).</summary>
    public static ChatMessage User(IReadOnlyList<ContentBlock> content) =>
        new("user", content);

    /// <summary>Creates an assistant message with the given content blocks.</summary>
    public static ChatMessage Assistant(IReadOnlyList<ContentBlock> content) =>
        new("assistant", content);
}

// ── LLM streaming response chunks (raw provider output) ─────────────────────

/// <summary>Discriminated union base for chunks streamed from an LLM provider.</summary>
public abstract record LlmChunk;

/// <summary>A text token delta emitted by the model.</summary>
public sealed record TextDeltaLlmChunk(string Delta) : LlmChunk;

/// <summary>A complete tool-use block returned by the model.</summary>
public sealed record ToolUseLlmChunk(string Id, string Name, string InputJson) : LlmChunk;

// ── QueryChunk – what QueryEngine yields to callers ──────────────────────────

/// <summary>Discriminated union base for chunks yielded by <see cref="QueryEngine"/>.</summary>
public abstract record QueryChunk;

/// <summary>A streamed text token produced by the model.</summary>
public sealed record TextQueryChunk(string Text) : QueryChunk;

/// <summary>Signals that the engine is about to execute a tool.</summary>
public sealed record ToolUseQueryChunk(string Id, string Name, string InputJson) : QueryChunk;

/// <summary>The result returned from a tool execution.</summary>
public sealed record ToolResultQueryChunk(string ToolUseId, string Result, bool IsError) : QueryChunk;

/// <summary>Token usage reported by the provider at the end of a response turn.</summary>
public sealed record UsageQueryChunk(int PromptTokens, int CompletionTokens) : QueryChunk;

// ── Provider exception ───────────────────────────────────────────────────────

/// <summary>
/// Thrown by <see cref="ILlmChatClient"/> implementations when the LLM provider
/// returns an HTTP error status code.
/// </summary>
public sealed class LlmProviderException : Exception
{
    /// <summary>The HTTP status code returned by the provider.</summary>
    public int StatusCode { get; }

    public LlmProviderException(int statusCode, string message)
        : base(message) => StatusCode = statusCode;

    public LlmProviderException(int statusCode, string message, Exception inner)
        : base(message, inner) => StatusCode = statusCode;
}

// ── Provider interfaces ──────────────────────────────────────────────────────

/// <summary>
/// Streams chat responses from an LLM provider given a conversation history.
/// </summary>
public interface ILlmChatClient
{
    /// <summary>
    /// Streams response chunks for the provided message history.
    /// </summary>
    /// <param name="messages">The full conversation history to send.</param>
    /// <param name="systemPrompt">Optional system prompt prepended to the request.</param>
    /// <param name="cancellationToken">Token to cancel the streaming operation.</param>
    /// <returns>An async sequence of <see cref="LlmChunk"/> values.</returns>
    IAsyncEnumerable<LlmChunk> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes a tool by name and returns its string output.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes the named tool with the given JSON-encoded input.
    /// </summary>
    /// <param name="toolName">The tool name.</param>
    /// <param name="inputJson">JSON-encoded tool input parameters.</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>The tool's string output.</returns>
    Task<string> ExecuteAsync(
        string toolName,
        string inputJson,
        CancellationToken cancellationToken = default);
}
