using System;
using System.Collections.Generic;

namespace WinRunner.Core.Models;

/// <summary>
/// Information about a detected Wine installation on the host system.
/// </summary>
public sealed class WineInfo
{
    /// <summary>Path to the wine binary.</summary>
    public string WinePath { get; set; } = string.Empty;

    /// <summary>Path to the wineserver binary (may be empty if not found).</summary>
    public string? WineserverPath { get; set; }

    /// <summary>Parsed version string, e.g. "wine-9.0".</summary>
    public string? Version { get; set; }

    /// <summary>True if the wine binary was found and executable.</summary>
    public bool IsAvailable { get; set; }

    /// <summary>True if 64-bit support is present.</summary>
    public bool Supports64Bit { get; set; }

    /// <summary>True if 32-bit support is present.</summary>
    public bool Supports32Bit { get; set; }

    /// <summary>Set when the binary is found but the version could not be parsed.</summary>
    public bool VersionUnknown { get; set; }

    /// <summary>Stderr captured when probing the binary (for diagnostics when unavailable).</summary>
    public string? ProbeError { get; set; }

    public override string ToString() =>
        $"{WinePath} ({Version ?? "version unknown"})";
}

/// <summary>
/// The set of detected compatibility backends available on the host.
/// </summary>
public sealed class CompatibilityRuntime
{
    public WineInfo? Wine { get; set; }

    /// <summary>True if any usable backend was found.</summary>
    public bool AnyAvailable => Wine?.IsAvailable == true;
}

/// <summary>
/// The result of a compatibility check, describing what the host can and cannot do.
/// </summary>
public sealed class CompatibilityCheckResult
{
    public bool WineInstalled { get; set; }
    public List<string> Messages { get; } = new();
    public CompatibilityRuntime Runtime { get; } = new();
}
