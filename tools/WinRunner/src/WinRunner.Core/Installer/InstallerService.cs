using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WinRunner.Core.Models;
using WinRunner.Core.Processes;
using WinRunner.Core.Wine;

namespace WinRunner.Core.Installer;

/// <summary>
/// Installs Windows applications (.exe / .msi) into isolated Wine prefixes.
/// </summary>
public sealed class InstallerService
{
    private readonly ProcessRunner _runner;
    private readonly PrefixManager _prefixes;

    public InstallerService(ProcessRunner runner, PrefixManager prefixes)
    {
        _runner = runner;
        _prefixes = prefixes;
    }

    /// <summary>Installs an installer file into a fresh prefix for the given app.</summary>
    public async Task<InstallResult> InstallAsync(
        string winePath,
        string appId,
        string installerPath,
        string architecture,
        IReadOnlyDictionary<string, string>? extraEnvironment = null,
        IReadOnlyList<string>? extraArguments = null,
        IProgress<string>? progress = null)
    {
        if (!File.Exists(installerPath))
        {
            return InstallResult.Failed(-1, $"Installer file not found: {installerPath}");
        }

        var extension = Path.GetExtension(installerPath).ToLowerInvariant();
        if (extension != ".exe" && extension != ".msi")
        {
            return InstallResult.Failed(-1, $"Unsupported installer type: '{extension}'. Expected .exe or .msi.");
        }

        // Initialize the prefix first.
        progress?.Report("Initializing Wine prefix...");
        var init = await _prefixes.InitializePrefixAsync(winePath, appId, architecture, extraEnvironment, progress);
        if (init.ExitCode != 0 && init.ExitCode != -1)
        {
            return InstallResult.Failed(init.ExitCode, "Failed to initialize Wine prefix: " + init.StandardError.Trim());
        }

        progress?.Report($"Installing {Path.GetFileName(installerPath)}...");

        var prefixDir = _prefixes.GetPrefixDirectory(appId);
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

        var args = new List<string>();

        if (extension == ".msi")
        {
            // Use msiexec to install the MSI silently.
            args.Add("msiexec");
            args.Add("/i");
            args.Add(ToWindowsPath(installerPath));
            args.Add("/qn");
            args.Add("/norestart");
        }
        else
        {
            // Run the .exe installer inside the prefix.
            args.Add(ToWindowsPath(installerPath));
        }

        if (extraArguments != null)
        {
            foreach (var a in extraArguments) args.Add(a);
        }

        var result = await _runner.RunAsync(
            winePath,
            args,
            environment: env,
            timeout: TimeSpan.FromMinutes(30));

        if (result.ExitCode != 0)
        {
            return InstallResult.Failed(result.ExitCode,
                "Installation returned a non-zero exit code. Some Windows installers do this even on success; " +
                "if the app was installed, you can continue. Details: " + result.StandardError.Trim());
        }

        return InstallResult.Succeeded(result, appId);
    }

    /// <summary>Converts a host path to a Windows-style path under the prefix's Z: drive (wine maps / to Z:\).</summary>
    public static string ToWindowsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        var normalized = path.Replace('/', '\\');
        if (Path.IsPathRooted(path) && normalized.StartsWith('\\'))
        {
            normalized = "Z:" + normalized;
        }
        return normalized;
    }
}
