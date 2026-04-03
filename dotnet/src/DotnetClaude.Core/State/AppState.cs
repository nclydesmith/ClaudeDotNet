namespace DotnetClaude.Core.State;

public record AppState(
    string ActiveTool = "None",
    int TokenCount = 0,
    bool IsStreaming = false);
