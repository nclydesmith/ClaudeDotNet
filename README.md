# OpenClaude .NET

[![.NET CI](https://github.com/nclydesmith/ClaudeDotNet/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/nclydesmith/ClaudeDotNet/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

OpenClaude .NET is a high-performance, native-compiled command-line interface (CLI) for agentic AI. Built from the ground up using **.NET 10**, it provides a blazing-fast, cross-platform, and extensible experience for interacting with over 200+ large language models.

---

## 🚀 Key Features

*   **Native Performance:** Compiled to native binary via .NET AOT, ensuring near-instant startup and minimal memory footprint.
*   **Universal Model Access:** Seamless integration with OpenAI, Anthropic, Gemini, DeepSeek, Ollama, and any other provider supporting standard APIs.
*   **Rich Terminal UI:** Interactive REPL and status indicators powered by [Spectre.Console](https://spectreconsole.net/).
*   **Modular Tooling:** Extensible architecture allows you to add custom tools (Bash, PowerShell, File I/O, WebSearch) with ease.
*   **Production-Ready:** Built-in telemetry-ready architecture, structured logging, and robust error handling.

## 🛠 Prerequisites

*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## 🏗 Build & Run

Clone the repository and build the solution:

```bash
git clone https://github.com/nclydesmith/ClaudeDotNet
cd ClaudeDotNet/dotnet
dotnet build OpenClaude.sln
```

Launch the CLI directly:

```bash
dotnet run --project src/DotnetClaude.Cli/DotnetClaude.Cli.csproj
```

## 📝 Slash Commands

OpenClaude .NET includes a built-in slash command router for local agent management:

| Command | Description |
| :--- | :--- |
| `/help` | List all available slash commands. |
| `/config` | Inspect or modify local configuration settings. |
| `/provider` | Switch between different LLM provider profiles. |

## 🏗 Architecture

The project is structured into distinct, decoupled domains:

- **DotnetClaude.Cli:** Entry point, REPL loop, and terminal rendering.
- **DotnetClaude.Core:** The heart of the agent: query engine, immutable state management, and orchestration.
- **DotnetClaude.Tools:** Extensible tool framework and built-in implementations (Bash, File I/O, Web Tools).
- **DotnetClaude.Providers:** Abstraction layer for LLM service providers.
- **DotnetClaude.Mcp:** Model Context Protocol (MCP) client scaffolding.

## ⚖️ License

This project is licensed under the [MIT License](LICENSE).
