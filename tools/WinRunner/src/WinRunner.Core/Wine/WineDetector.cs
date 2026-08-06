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
/// Locates Wine on the host and probes its capabilities (version, architecture support).
/// </summary>
public sealed class WineDetector
{
    private readonly ProcessRunner _runner;

    public WineDetector(ProcessRunner runner)
    {
        _runner = runner;
    }

    /// <summary>
    /// Searches the PATH plus a set of well-known install locations for a wine binary.
    /// </summary>
    public async Task<WineInfo> DetectAsync()
    {
        var candidates = new List<string>();

        // Search PATH first.
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var name in new[] { "wine", "wine64", "wine-staging", "wine-stable" })
            {
                var full = Path.Combine(dir.Trim(), name);
                if (File.Exists(full))
                {
                    candidates.Add(full);
                }
            }
        }

        // Well-known locations (Linux + macOS homebrew).
        var known = new[]
        {
            "/usr/bin/wine", "/usr/local/bin/wine", "/opt/wine/bin/wine",
            "/usr/bin/wine64", "/usr/local/bin/wine64",
            Environment.GetEnvironmentVariable("HOME") + "/.wine/bin/wine",
            "/Applications/Wine.app/Contents/Resources/wine/bin/wine",
            "/Applications/Wine Stable.app/Contents/Resources/wine/bin/wine",
            "/Applications/Wine Devel.app/Contents/Resources/wine/bin/wine",
        };
        foreach (var k in known)
        {
            if (!string.IsNullOrEmpty(k) && File.Exists(k))
            {
                candidates.Add(k);
            }
        }

        var info = new WineInfo { IsAvailable = false };
        foreach (var candidate in candidates.Distinct())
        {
            var probe = await ProbeAsync(candidate);
            if (probe.IsAvailable)
            {
                return probe;
            }
            // Keep the last candidate's partial info so we can report why it failed.
            info = probe;
        }

        return info;
    }

    private async Task<WineInfo> ProbeAsync(string winePath)
    {
        var info = new WineInfo
        {
            WinePath = winePath,
            IsAvailable = false,
        };

        var result = await _runner.RunAsync(winePath, new[] { "--version" }, timeout: TimeSpan.FromSeconds(20));

        if (result.ExitCode != 0)
        {
            info.ProbeError = result.StandardError.Trim();
            return info;
        }

        var versionLine = (result.StandardOutput + result.StandardError)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        info.Version = ParseVersion(versionLine);
        info.IsAvailable = true;
        info.VersionUnknown = string.IsNullOrEmpty(info.Version);

        // Determine architecture support by looking for the 64-bit loader next to the binary.
        var dir = Path.GetDirectoryName(winePath);
        if (dir != null)
        {
            info.Supports64Bit = File.Exists(Path.Combine(dir, "wine64"))
                || Path.GetFileName(winePath).Contains("64", StringComparison.OrdinalIgnoreCase);
            // 32-bit is supported by any standard wine build (WOW64). Assume true unless we can't tell.
            info.Supports32Bit = true;
            var ws = Path.Combine(dir, "wineserver");
            if (File.Exists(ws)) info.WineserverPath = ws;
        }

        return info;
    }

    /// <summary>Extracts the version string from a `wine --version` output line.</summary>
    public static string? ParseVersion(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        // Examples: "wine-9.0", "wine-9.0 (Staging)", "wine-8.0.2"
        var m = Regex.Match(line, @"wine-(\d+(?:\.\d+)*)");
        return m.Success ? "wine-" + m.Groups[1].Value : null;
    }
}
