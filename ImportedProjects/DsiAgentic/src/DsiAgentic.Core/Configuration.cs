using System.Text.Json;

namespace DsiAgentic.Core;

public static class Configuration
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static RootConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<RootConfig>(json, Opts)
                  ?? throw new InvalidOperationException("Failed to parse config");
        return cfg;
    }

    public static void Save(RootConfig cfg, string path)
        => File.WriteAllText(path, JsonSerializer.Serialize(cfg, Opts));

    public static JsonSerializerOptions JsonOptions => Opts;
}
