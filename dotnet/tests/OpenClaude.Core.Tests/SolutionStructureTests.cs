namespace OpenClaude.Core.Tests;

/// <summary>
/// Validates the .NET solution project structure.
/// Ensures all expected project files exist and are reachable from the solution root.
/// </summary>
public class SolutionStructureTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate solution root from " + AppContext.BaseDirectory);
    }

    [Theory]
    [InlineData("src/OpenClaude.Core/OpenClaude.Core.csproj")]
    [InlineData("src/OpenClaude.Providers/OpenClaude.Providers.csproj")]
    [InlineData("src/OpenClaude.Tools/OpenClaude.Tools.csproj")]
    [InlineData("src/OpenClaude.Mcp/OpenClaude.Mcp.csproj")]
    [InlineData("src/OpenClaude.Cli/OpenClaude.Cli.csproj")]
    [InlineData("tests/OpenClaude.Core.Tests/OpenClaude.Core.Tests.csproj")]
    [InlineData("Directory.Build.props")]
    public void ProjectFileExists(string relativePath)
    {
        var fullPath = Path.Combine(SolutionRoot, relativePath);
        Assert.True(File.Exists(fullPath), $"Expected project file not found: {fullPath}");
    }

    [Fact]
    public void DirectoryBuildPropsEnablesNullable()
    {
        var propsPath = Path.Combine(SolutionRoot, "Directory.Build.props");
        var content = File.ReadAllText(propsPath);
        Assert.Contains("<Nullable>enable</Nullable>", content);
    }

    [Fact]
    public void DirectoryBuildPropsEnablesImplicitUsings()
    {
        var propsPath = Path.Combine(SolutionRoot, "Directory.Build.props");
        var content = File.ReadAllText(propsPath);
        Assert.Contains("<ImplicitUsings>enable</ImplicitUsings>", content);
    }
}
