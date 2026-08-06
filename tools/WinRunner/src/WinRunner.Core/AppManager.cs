using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinRunner.Core.Configuration;
using WinRunner.Core.Installer;
using WinRunner.Core.Launch;
using WinRunner.Core.Models;
using WinRunner.Core.Processes;
using WinRunner.Core.Wine;

namespace WinRunner.Core;

/// <summary>
/// High-level facade that coordinates Wine detection, prefix management,
/// installation, launching, and persistence. Used by both the CLI and GUI.
/// </summary>
public sealed class AppManager
{
    private readonly ProcessRunner _runner;
    private readonly SettingsManager _settings;
    private readonly PrefixManager _prefixes;
    private readonly WineDetector _detector;
    private readonly InstallerService _installer;
    private readonly AppLauncher _launcher;

    public AppManager(string dataDirectory)
    {
        _runner = new ProcessRunner();
        _settings = new SettingsManager(dataDirectory);
        _prefixes = new PrefixManager(_runner, Path.Combine(dataDirectory, "prefixes"));
        _detector = new WineDetector(_runner);
        _installer = new InstallerService(_runner, _prefixes);
        _launcher = new AppLauncher(_runner, _prefixes);
    }

    public SettingsManager Settings => _settings;
    public PrefixManager Prefixes => _prefixes;

    /// <summary>Detects the Wine runtime on the host.</summary>
    public Task<WineInfo> DetectWineAsync() => _detector.DetectAsync();

    /// <summary>Runs a full compatibility check and returns a human-readable report.</summary>
    public async Task<CompatibilityCheckResult> CheckCompatibilityAsync()
    {
        var result = new CompatibilityCheckResult();
        var wine = await _detector.DetectAsync();

        if (wine.IsAvailable)
        {
            result.WineInstalled = true;
            result.Runtime.Wine = wine;
            result.Messages.Add($"Wine found at {wine.WinePath} ({(wine.Version ?? "version unknown")}).");
            if (wine.Supports64Bit) result.Messages.Add("64-bit support detected.");
            if (wine.Supports32Bit) result.Messages.Add("32-bit support detected.");
        }
        else
        {
            result.Messages.Add(
                "Wine was not found. Install Wine first: on Linux use your package manager " +
                "(e.g. 'sudo apt install wine64' or 'sudo dnf install wine'), on macOS install " +
                "Wine via Homebrew ('brew install --cask wine-stable') or download from winehq.org.");
            if (!string.IsNullOrEmpty(wine.ProbeError))
            {
                result.Messages.Add("Probe error: " + wine.ProbeError);
            }
        }

        return result;
    }

    /// <summary>Lists all installed apps.</summary>
    public async Task<IReadOnlyList<InstalledApp>> ListAppsAsync()
    {
        var apps = await _settings.LoadAppsAsync();
        return apps.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Gets a single app by id, or null.</summary>
    public async Task<InstalledApp?> GetAppAsync(string id)
    {
        var apps = await _settings.LoadAppsAsync();
        return apps.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Registers a new app and installs its installer into an isolated prefix.
    /// </summary>
    public async Task<InstallResult> InstallAppAsync(
        string name,
        string installerPath,
        string? executablePath,
        string architecture = "win64",
        IReadOnlyDictionary<string, string>? environment = null,
        IReadOnlyList<string>? installerArguments = null,
        IProgress<string>? progress = null)
    {
        var wine = await _detector.DetectAsync();
        if (!wine.IsAvailable)
        {
            return InstallResult.Failed(-1,
                "Wine is not installed on this system. Install Wine first (see 'check' command).");
        }

        var id = PrefixManager.Slugify(name);
        if (string.IsNullOrWhiteSpace(id)) id = "app-" + Guid.NewGuid().ToString("N")[..8];

        // Avoid id collisions.
        var existing = await _settings.LoadAppsAsync();
        if (existing.Any(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            id = id + "-" + Guid.NewGuid().ToString("N")[..4];
        }

        progress?.Report($"Installing '{name}' (id: {id})...");
        var result = await _installer.InstallAsync(
            wine.WinePath, id, installerPath, architecture, environment, installerArguments, progress);

        if (!result.Success)
        {
            // Clean up the partial prefix on failure.
            _prefixes.DeletePrefix(id);
            return result;
        }

        var app = new InstalledApp
        {
            Id = id,
            Name = name,
            InstallerPath = installerPath,
            ExecutablePath = executablePath,
            Architecture = architecture,
            InstalledAtUtc = DateTime.UtcNow,
            EnvironmentVariables = environment is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase),
        };

        existing.Add(app);
        await _settings.SaveAppsAsync(existing);
        return result;
    }

    /// <summary>Launches an installed app by id.</summary>
    public async Task<LaunchResult> LaunchAppAsync(string id, bool waitForExit = false)
    {
        var app = await GetAppAsync(id);
        if (app == null) return LaunchResult.Fail($"No app found with id '{id}'.");

        var wine = await _detector.DetectAsync();
        if (!wine.IsAvailable) return LaunchResult.Fail("Wine is not installed on this system.");

        var result = _launcher.Launch(app, wine.WinePath, waitForExit);
        if (result.Success)
        {
            // Update launch tracking.
            app.LastLaunchedAtUtc = DateTime.UtcNow;
            app.LaunchCount++;
            var apps = await _settings.LoadAppsAsync();
            var idx = apps.FindIndex(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                apps[idx].LastLaunchedAtUtc = app.LastLaunchedAtUtc;
                apps[idx].LaunchCount = app.LaunchCount;
                await _settings.SaveAppsAsync(apps);
            }
        }
        return result;
    }

    /// <summary>Updates an app's metadata (executable path, args, env, notes).</summary>
    public async Task<bool> UpdateAppAsync(string id, Action<InstalledApp> mutate)
    {
        var apps = await _settings.LoadAppsAsync();
        var idx = apps.FindIndex(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;
        mutate(apps[idx]);
        await _settings.SaveAppsAsync(apps);
        return true;
    }

    /// <summary>Uninstalls an app: removes its prefix and registry entry.</summary>
    public async Task<bool> UninstallAppAsync(string id)
    {
        var apps = await _settings.LoadAppsAsync();
        var idx = apps.FindIndex(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;

        _prefixes.DeletePrefix(id);
        apps.RemoveAt(idx);
        await _settings.SaveAppsAsync(apps);
        return true;
    }
}
