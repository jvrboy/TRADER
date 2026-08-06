using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinRunner.Core.Models;
using WinRunner.Core.Processes;

namespace WinRunner.Core.Wine;

/// <summary>
/// Manages isolated Wine prefixes. Each installed app gets its own prefix
/// directory so applications don't interfere with each other.
/// </summary>
public sealed class PrefixManager
{
    private readonly ProcessRunner _runner;
    private readonly string _prefixRoot;

    public PrefixManager(ProcessRunner runner, string prefixRoot)
    {
        _runner = runner;
        _prefixRoot = prefixRoot;
    }

    /// <summary>Root directory under which all prefixes live.</summary>
    public string PrefixRoot => _prefixRoot;

    /// <summary>Resolves the prefix directory for a given app id.</summary>
    public string GetPrefixDirectory(string appId)
    {
        var safe = Slugify(appId);
        return Path.Combine(_prefixRoot, safe);
    }

    /// <summary>Resolves the path to drive_c inside a prefix.</summary>
    public string GetDriveCDirectory(string appId) => Path.Combine(GetPrefixDirectory(appId), "drive_c");

    /// <summary>
    /// Initializes (creates if needed) a fresh Wine prefix for an app.
    /// Runs `wineboot` to set up the prefix skeleton.
    /// </summary>
    public async Task<ProcessResult> InitializePrefixAsync(
        string winePath,
        string appId,
        string architecture,
        IReadOnlyDictionary<string, string>? extraEnvironment = null,
        IProgress<string>? progress = null)
    {
        var prefixDir = GetPrefixDirectory(appId);
        Directory.CreateDirectory(prefixDir);

        progress?.Report($"Initializing Wine prefix for {appId}...");

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WINEPREFIX"] = prefixDir,
            ["WINEDEBUG"] = "-all",
        };
        if (architecture.Equals("win32", StringComparison.OrdinalIgnoreCase))
        {
            env["WINEARCH"] = "win32";
        }
        if (extraEnvironment != null)
        {
            foreach (var kv in extraEnvironment) env[kv.Key] = kv.Value;
        }

        // wineboot -i initializes the prefix non-interactively.
        var result = await _runner.RunAsync(
            winePath,
            new[] { "wineboot", "-i" },
            environment: env,
            timeout: TimeSpan.FromMinutes(5));

        return result;
    }

    /// <summary>Checks whether a prefix already exists (and is initialized) for an app.</summary>
    public bool PrefixExists(string appId)
    {
        var driveC = GetDriveCDirectory(appId);
        return Directory.Exists(driveC) && Directory.Exists(Path.Combine(driveC, "windows"));
    }

    /// <summary>Deletes a prefix directory tree.</summary>
    public void DeletePrefix(string appId)
    {
        var dir = GetPrefixDirectory(appId);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>Lists all existing prefix directories.</summary>
    public IReadOnlyList<string> ListPrefixes()
    {
        if (!Directory.Exists(_prefixRoot)) return Array.Empty<string>();
        return Directory.GetDirectories(_prefixRoot)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>()
            .ToList();
    }

    /// <summary>Gets the total size on disk of a prefix in bytes.</summary>
    public long GetPrefixSize(string appId)
    {
        var dir = GetPrefixDirectory(appId);
        if (!Directory.Exists(dir)) return 0;
        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f =>
        {
            try { return new FileInfo(f).Length; } catch { return 0L; }
        });
    }

    /// <summary>Converts a display name into a filesystem-safe slug used as the app id and prefix name.</summary>
    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "app";
        var slug = value.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? "app" : slug;
    }
}
