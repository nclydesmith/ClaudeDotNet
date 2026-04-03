using System.Text.Json;

namespace DotnetClaude.Core.Config;

public class SettingsStore
{
    private readonly string _path;
    public SettingsStore(string path) => _path = path;

    public ProfileConfig Load()
    {
        if (!File.Exists(_path)) return new ProfileConfig();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<ProfileConfig>(json) ?? new ProfileConfig();
    }
}

public record ProfileConfig(string ApiKey = "", string Model = "claude-3-5-sonnet");
