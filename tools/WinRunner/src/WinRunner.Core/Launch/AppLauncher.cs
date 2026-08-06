using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WinRunner.Core.Models;
using WinRunner.Core.Processes;
using WinRunner.Core.Wine;

namespace WinRunner.Core.Launch;

/// <summary>Outcome of launching an installed application.</summary>
public sealed class LaunchResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool LaunchedDetached { get; private set; }

    public static LaunchResult Ok(bool detached) =>
        new() { Success = true, LaunchedDetached = detached };

    public static LaunchResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Launches installed Windows applications inside their Wine prefixes.
/// </summary>
public sealed class AppLauncher
{
    private readonly ProcessRunner _runner;
    private readonly PrefixManager _prefixes;

    public AppLauncher(ProcessRunner runner, PrefixManager prefixes)
    {
        _runner = runner;
        _prefixes = prefixes;
    }

    /// <summary>
    /// Launches an installed app's executable. Returns immediately (detached) so
    /// the GUI/CLI isn't blocked while the Windows app is running.
    /// </summary>
    public LaunchResult Launch(InstalledApp app, string winePath, bool waitForExit = false)
    {
        if (string.IsNullOrWhiteSpace(app.ExecutablePath))
        {
            return LaunchResult.Fail("No executable path is configured for this app.");
        }

        var prefixDir = _prefixes.GetPrefixDirectory(app.Id);
        if (!Directory.Exists(prefixDir))
        {
            return LaunchResult.Fail($"Prefix does not exist for '{app.Name}'. Reinstall the app.");
        }

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WINEPREFIX"] = prefixDir,
            ["WINEDEBUG"] = "-all",
        };
        if (app.Architecture.Equals("win32", StringComparison.OrdinalIgnoreCase))
        {
            env["WINEARCH"] = "win32";
        }
        foreach (var kv in app.EnvironmentVariables)
        {
            env[kv.Key] = kv.Value;
        }

        var args = new List<string>();

        // Resolve the executable path. It may be a Windows-style path (C:\...) or a host path.
        var exe = app.ExecutablePath;
        if (LooksLikeWindowsPath(exe))
        {
            args.Add(exe);
        }
        else
        {
            // If it's a host path, pass it through as-is (wine maps Z:\).
            args.Add(ToWineArg(exe));
        }

        if (!string.IsNullOrWhiteSpace(app.LaunchArguments))
        {
            foreach (var part in SplitArguments(app.LaunchArguments))
            {
                args.Add(part);
            }
        }

        if (waitForExit)
        {
            // Blocking launch — used by CLI when the user wants to see the result.
            var result = _runner.RunAsync(winePath, args, environment: env).GetAwaiter().GetResult();
            return result.ExitCode == 0
                ? LaunchResult.Ok(false)
                : LaunchResult.Fail("The application exited with a non-zero code.");
        }

        var started = _runner.LaunchDetached(winePath, args, environment: env);
        return started ? LaunchResult.Ok(true) : LaunchResult.Fail("Failed to start the application process.");
    }

    private static bool LooksLikeWindowsPath(string path) =>
        path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/');

    private static string ToWineArg(string path) =>
        Path.IsPathRooted(path) ? "Z:" + path.Replace('/', '\\') : path;

    private static IEnumerable<string> SplitArguments(string args)
    {
        // Minimal argument splitter handling quoted segments.
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        foreach (var c in args)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0) result.Add(current.ToString());
        return result;
    }
}
