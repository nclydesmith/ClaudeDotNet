namespace DotnetClaude.Tools;

/// <summary>
/// Thrown when a tool with the same <see cref="ITool.Name"/> is registered more than once.
/// </summary>
public sealed class DuplicateToolException : Exception
{
    /// <summary>The name of the tool that caused the duplicate registration.</summary>
    public string ToolName { get; }

    /// <inheritdoc cref="DuplicateToolException"/>
    public DuplicateToolException(string toolName)
        : base($"A tool named '{toolName}' is already registered.")
        => ToolName = toolName;
}

/// <summary>
/// Thrown when <see cref="ToolRegistry.Resolve"/> is called with a name that has not been registered.
/// </summary>
public sealed class ToolNotFoundException : Exception
{
    /// <summary>The name that was looked up and not found.</summary>
    public string ToolName { get; }

    /// <inheritdoc cref="ToolNotFoundException"/>
    public ToolNotFoundException(string toolName)
        : base($"No tool named '{toolName}' is registered.")
        => ToolName = toolName;
}

/// <summary>
/// Central registry for <see cref="ITool"/> implementations.
/// Tools are keyed by <see cref="ITool.Name"/>; names are case-sensitive.
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = [];

    /// <summary>
    /// Registers a tool so it can be resolved by name.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tool"/> is <see langword="null"/>.</exception>
    /// <exception cref="DuplicateToolException">Thrown when a tool with the same name is already registered.</exception>
    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (_tools.ContainsKey(tool.Name))
            throw new DuplicateToolException(tool.Name);

        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Returns the tool registered under <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The case-sensitive tool name to look up.</param>
    /// <returns>The registered <see cref="ITool"/>.</returns>
    /// <exception cref="ToolNotFoundException">Thrown when no tool with that name has been registered.</exception>
    public ITool Resolve(string name)
    {
        if (_tools.TryGetValue(name, out ITool? tool))
            return tool;

        throw new ToolNotFoundException(name);
    }

    /// <summary>Returns a snapshot of all currently registered tools.</summary>
    public IReadOnlyCollection<ITool> GetAll() => _tools.Values;
}
