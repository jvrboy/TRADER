using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinRunner.Core.Models;

namespace WinRunner.Core.Configuration;

/// <summary>
/// Persists the catalog of installed apps and global settings to JSON on disk.
/// </summary>
public sealed class SettingsManager
{
    private readonly string _dataDir;
    private readonly string _registryPath;
    private readonly string _settingsPath;

    public SettingsManager(string dataDirectory)
    {
        _dataDir = dataDirectory;
        _registryPath = Path.Combine(_dataDir, "apps.json");
        _settingsPath = Path.Combine(_dataDir, "settings.json");
    }

    public string DataDirectory => _dataDir;

    public string RegistryPath => _registryPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Loads all installed apps. Returns an empty list if none exist.</summary>
    public async Task<List<InstalledApp>> LoadAppsAsync()
    {
        if (!File.Exists(_registryPath)) return new List<InstalledApp>();
        try
        {
            var json = await File.ReadAllTextAsync(_registryPath);
            var apps = JsonSerializer.Deserialize<List<InstalledApp>>(json, JsonOptions);
            return apps ?? new List<InstalledApp>();
        }
        catch (JsonException)
        {
            return new List<InstalledApp>();
        }
    }

    /// <summary>Saves the full list of installed apps.</summary>
    public async Task SaveAppsAsync(IEnumerable<InstalledApp> apps)
    {
        Directory.CreateDirectory(_dataDir);
        var json = JsonSerializer.Serialize(apps, JsonOptions);
        await File.WriteAllTextAsync(_registryPath, json);
    }

    /// <summary>Loads global settings, creating defaults if none exist.</summary>
    public async Task<GlobalSettings> LoadSettingsAsync()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var s = JsonSerializer.Deserialize<GlobalSettings>(json, JsonOptions);
                if (s != null) return s;
            }
            catch (JsonException) { }
        }
        return new GlobalSettings();
    }

    /// <summary>Saves global settings.</summary>
    public async Task SaveSettingsAsync(GlobalSettings settings)
    {
        Directory.CreateDirectory(_dataDir);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json);
    }
}

/// <summary>Global application settings.</summary>
public sealed class GlobalSettings
{
    /// <summary>Default Wine architecture for new prefixes.</summary>
    public string DefaultArchitecture { get; set; } = "win64";

    /// <summary>Root directory for all prefixes.</summary>
    public string? PrefixRoot { get; set; }

    /// <summary>Preferred Wine binary override. Empty = auto-detect.</summary>
    public string? WineBinaryOverride { get; set; }

    /// <summary>Whether to auto-initialize a prefix on first install.</summary>
    public bool AutoInitializePrefix { get; set; } = true;
}
