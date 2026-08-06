using System;

namespace WinRunner.Core.Models;

/// <summary>
/// A Windows application that has been registered with WinRunner.
/// Each app lives in its own isolated Wine prefix.
/// </summary>
public sealed class InstalledApp
{
    /// <summary>Stable unique identifier for the app (a slug).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the application.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Path to the original installer (.exe or .msi) that was used.</summary>
    public string? InstallerPath { get; set; }

    /// <summary>Path to the main executable to launch (relative to the prefix drive_c).</summary>
    public string? ExecutablePath { get; set; }

    /// <summary>Arguments passed to the executable when launching.</summary>
    public string? LaunchArguments { get; set; }

    /// <summary>Wine architecture for this app's prefix: "win64" or "win32".</summary>
    public string Architecture { get; set; } = "win64";

    /// <summary>Preferred Wine version override (e.g. "wine-staging"). Empty = use default.</summary>
    public string? WineVersionPreference { get; set; }

    /// <summary>Optional extra environment variables applied when launching.</summary>
    public System.Collections.Generic.Dictionary<string, string> EnvironmentVariables { get; set; } =
        new(System.StringComparer.OrdinalIgnoreCase);

    /// <summary>UTC timestamp when the app was registered.</summary>
    public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last successful launch.</summary>
    public DateTime? LastLaunchedAtUtc { get; set; }

    /// <summary>Number of times the app has been launched.</summary>
    public long LaunchCount { get; set; }

    /// <summary>Notes the user may attach to the app.</summary>
    public string? Notes { get; set; }
}
