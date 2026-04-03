# Build Progress

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
