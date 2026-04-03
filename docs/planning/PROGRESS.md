# Build Progress

## 2026-04-03

### SUB-005
- **Problem:** No built-in file-system tools existed; needed `FileReadTool`, `FileWriteTool`, `FileEditTool`, `GlobTool`, and `GrepTool` as concrete `ITool` implementations so agents can read, write, edit, and search the local filesystem.
- **Changes:**
  - Updated `ToolResult` with computed `Content` (alias for `Output`), `IsError` bool, and init-only `Metadata` dictionary — backward-compatible with existing SUB-004 tests.
  - Added `Microsoft.Extensions.FileSystemGlobbing` NuGet reference to `OpenClaude.Tools.csproj`.
  - Created `FileReadTool`: reads file lines, returns content prefixed with 6-digit right-aligned line numbers (`{n,6}\t{line}`); supports `offset`/`limit` windowing.
  - Created `FileWriteTool`: writes content to disk, creating parent directories as needed; reports bytes written in metadata.
  - Created `FileEditTool`: exact-string replacement with uniqueness guard — errors when `old_string` appears more than once unless `replace_all: true`; returns replacement count in metadata.
  - Created `GlobTool`: wraps `Microsoft.Extensions.FileSystemGlobbing.Matcher` for `**`-style patterns; results sorted by last-write time (most-recently-modified first); returns match count in metadata.
  - Created `GrepTool`: regex search across files in a directory (filtered by optional `glob`); returns `filepath:linenum:content` lines; skips known binary extensions; returns file/match counts in metadata.
  - All five tools parse snake_case JSON inputs via `[JsonPropertyName]` attributes and return structured `ToolResult`.
  - Created 34 xUnit tests (temp-directory fixtures, no network) covering all acceptance criteria: line-number prefix, offset/limit windowing, edit uniqueness guard, replace_all, glob pattern matching, modification-time sort, regex matching, multi-file grep, glob filter, metadata fields, and error paths.
- **Files:**
  - Modified: `dotnet/src/OpenClaude.Tools/ToolResult.cs`, `dotnet/src/OpenClaude.Tools/OpenClaude.Tools.csproj`
  - Created: `dotnet/src/OpenClaude.Tools/BuiltIn/FileReadTool.cs`, `dotnet/src/OpenClaude.Tools/BuiltIn/FileWriteTool.cs`, `dotnet/src/OpenClaude.Tools/BuiltIn/FileEditTool.cs`, `dotnet/src/OpenClaude.Tools/BuiltIn/GlobTool.cs`, `dotnet/src/OpenClaude.Tools/BuiltIn/GrepTool.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/FileReadToolTests.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/FileEditToolTests.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/GlobToolTests.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/GrepToolTests.cs`

### SUB-004
- **Problem:** No tool system existed in the C# port; needed the scaffold (interfaces, registry, permission pipeline, progress sink) that all built-in tools plug into.
- **Changes:**
  - Created `ITool` interface: `Name`, `Description`, and `ExecuteAsync(ToolExecutionContext, CancellationToken)`.
  - Created `ToolResult` record with `ToolResultStatus` enum (Success/Denied/Error) and factory helpers `Succeeded`, `Denied`, `Error`.
  - Created `ToolExecutionContext` record carrying `ToolName`, `InputJson`, and `IToolProgressSink`.
  - Created `IToolProgressSink` interface and discriminated-union events: `ToolStartedEvent`, `ToolCompletedEvent`, `ToolProgressMessage`.
  - Created `ICanUseTool` interface: async `IsAllowedAsync` method called before execution.
  - Created `ToolPermissionPolicy` (AND-composition of multiple `ICanUseTool` checks) and `RateLimitingPolicy` (sliding-window rate limiter).
  - Created `ToolRegistry` with `Register`, `Resolve`, `GetAll`; throws `DuplicateToolException` / `ToolNotFoundException` on error.
  - Created `IToolExecutor` interface and `ToolExecutor` implementation: checks permission → emits `ToolStartedEvent` → calls tool → emits `ToolCompletedEvent`; returns `Denied` result without calling the tool when permission is refused.
  - Added `ProjectReference` to `OpenClaude.Tools` in the test project.
  - Created 19 xUnit tests covering all four acceptance criteria.
- **Files:**
  - Created: `dotnet/src/OpenClaude.Tools/ITool.cs`, `dotnet/src/OpenClaude.Tools/IToolExecutor.cs`, `dotnet/src/OpenClaude.Tools/ToolExecutor.cs`, `dotnet/src/OpenClaude.Tools/ToolRegistry.cs`, `dotnet/src/OpenClaude.Tools/ToolPermissionPolicy.cs`, `dotnet/src/OpenClaude.Tools/ICanUseTool.cs`, `dotnet/src/OpenClaude.Tools/IToolProgressSink.cs`, `dotnet/src/OpenClaude.Tools/ToolExecutionContext.cs`, `dotnet/src/OpenClaude.Tools/ToolResult.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/ToolRegistryTests.cs`, `dotnet/tests/OpenClaude.Core.Tests/Tools/ToolExecutorTests.cs`
  - Modified: `dotnet/tests/OpenClaude.Core.Tests/OpenClaude.Core.Tests.csproj`

## 2026-04-03

### SUB-003
- **Problem:** No query/response loop existed in the C# port; needed the core streaming engine that sends messages, processes tool-use blocks, retries on transient errors, and trims history.
- **Changes:**
  - Created `QueryOptions` record for per-run configuration (retry attempts, context window, base delay, system prompt).
  - Created `QueryResult.cs` containing all discriminated-union types: `ContentBlock` hierarchy (`TextBlock`, `ToolUseBlock`, `ToolResultBlock`), `ChatMessage`, `LlmChunk` hierarchy, `QueryChunk` hierarchy, `LlmProviderException`, `ILlmChatClient`, and `IToolExecutor`.
  - Created `RetryPolicy` static class: `IsQuotaExhausted()` (mirrors fix(retry) commit — detects "limit: 0" / "exceeded your current quota"), `IsTransient()` (429 non-quota + 503), `GetDelay()` (exponential backoff capped at 30 s).
  - Created `MessageHistory` with `Add()`, `GetAll()` (snapshot), `EstimateTokens()` (chars/4), and `Trim()` (removes oldest until under limit, keeping at least one message).
  - Created `QueryEngine` with `RunAsync(IReadOnlyList<ChatMessage>, CancellationToken)` → `IAsyncEnumerable<QueryChunk>`; inner `FetchWithRetryAsync` collects all LLM chunks before yielding, enabling clean retry semantics without mid-stream yield-in-catch.
  - Added `ProjectReference` to `OpenClaude.Core` in the test project.
  - Created 16 xUnit tests covering all four acceptance criteria.
- **Files:**
  - Created: `dotnet/src/OpenClaude.Core/Query/QueryOptions.cs`, `dotnet/src/OpenClaude.Core/Query/QueryResult.cs`, `dotnet/src/OpenClaude.Core/Query/RetryPolicy.cs`, `dotnet/src/OpenClaude.Core/Query/MessageHistory.cs`, `dotnet/src/OpenClaude.Core/Query/QueryEngine.cs`, `dotnet/tests/OpenClaude.Core.Tests/Query/QueryEngineTests.cs`
  - Modified: `dotnet/tests/OpenClaude.Core.Tests/OpenClaude.Core.Tests.csproj`

## 2026-04-03

### SUB-002
- **Problem:** No provider abstraction layer existed; needed C# interfaces and implementations for Anthropic and OpenAI-compatible providers, plus a resolver mirroring TypeScript `providerConfig.ts` detection logic.
- **Changes:**
  - Created `ILlmProvider` and `ILlmStreamingClient` interfaces in `OpenClaude.Providers`.
  - Created `ProviderConfig` record, `ProviderType` enum, and `ProviderConfigException` in `ProviderConfig.cs`.
  - Implemented `ProviderResolver.Resolve()` with env-var detection order: `CLAUDE_CODE_USE_OPENAI` → `GEMINI_API_KEY` → `OLLAMA_BASE_URL` → default Anthropic.
  - Implemented `AnthropicProvider` and `OpenAiCompatibleProvider`, both implementing `ILlmProvider` with nested `ILlmStreamingClient` stubs (SDK integration deferred to SUB-003).
  - Added `<ProjectReference>` from `OpenClaude.Core.Tests` to `OpenClaude.Providers`.
  - Added `<NoWarn>CA1707;CA1859</NoWarn>` to test project for xUnit naming convention compatibility.
  - Created 22 xUnit tests covering all four provider detection scenarios, env-var reading, and `ProviderConfigException` behaviour.
- **Files:**
  - Created: `dotnet/src/OpenClaude.Providers/ILlmProvider.cs`, `dotnet/src/OpenClaude.Providers/ILlmStreamingClient.cs`, `dotnet/src/OpenClaude.Providers/ProviderConfig.cs`, `dotnet/src/OpenClaude.Providers/ProviderResolver.cs`, `dotnet/src/OpenClaude.Providers/AnthropicProvider.cs`, `dotnet/src/OpenClaude.Providers/OpenAiCompatibleProvider.cs`, `dotnet/tests/OpenClaude.Core.Tests/Providers/ProviderResolverTests.cs`
  - Modified: `dotnet/tests/OpenClaude.Core.Tests/OpenClaude.Core.Tests.csproj`

## 2026-04-03

### SUB-001
- **Problem:** No .NET solution structure existed; needed to bootstrap projects, CI, and coding conventions for the dotnet port of OpenClaude.
- **Changes:**
  - Created `dotnet/OpenClaude.sln` with 6 projects (5 src + 1 test).
  - Created csproj files for `OpenClaude.Core`, `OpenClaude.Providers`, `OpenClaude.Tools`, `OpenClaude.Mcp`, `OpenClaude.Cli`, and `OpenClaude.Core.Tests`.
  - Created `dotnet/Directory.Build.props` enabling `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, and `<AnalysisLevel>latest-recommended</AnalysisLevel>` globally.
  - Created `dotnet/.editorconfig` with C# coding conventions (file-scoped namespaces, brace style, naming).
  - Created `SolutionStructureTests.cs` with 9 xUnit tests validating project file presence and Directory.Build.props content.
  - Created `.github/workflows/dotnet-ci.yml` with OS matrix (ubuntu, windows, macOS), triggering on push to `main` and all PRs.
  - Updated `.gitignore` with .NET bin/obj exclusions.
- **Files:**
  - Created: `dotnet/OpenClaude.sln`, `dotnet/src/OpenClaude.Cli/OpenClaude.Cli.csproj`, `dotnet/src/OpenClaude.Core/OpenClaude.Core.csproj`, `dotnet/src/OpenClaude.Providers/OpenClaude.Providers.csproj`, `dotnet/src/OpenClaude.Tools/OpenClaude.Tools.csproj`, `dotnet/src/OpenClaude.Mcp/OpenClaude.Mcp.csproj`, `dotnet/tests/OpenClaude.Core.Tests/OpenClaude.Core.Tests.csproj`, `dotnet/tests/OpenClaude.Core.Tests/SolutionStructureTests.cs`, `dotnet/Directory.Build.props`, `dotnet/.editorconfig`, `.github/workflows/dotnet-ci.yml`
  - Modified: `.gitignore`
