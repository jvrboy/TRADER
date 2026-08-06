using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinRunner.Core;

namespace WinRunner.Cli;

/// <summary>
/// Command-line entry point for WinRunner. Usage:
///   winrunner check
///   winrunner install <name> <installer> [--exe <path>] [--arch win64|win32]
///   winrunner list
///   winrunner launch <id> [--wait]
///   winrunner uninstall <id>
///   winrunner info <id>
///   winrunner set-exe <id> <path>
///   winrunner set-args <id> <arguments>
///   winrunner set-note <id> <note>
/// </summary>
public static class Program
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".winrunner");

    public static async Task<int> Main(string[] args)
    {
        try
        {
            return await RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var manager = new AppManager(DataDir);
        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "check":
            case "status":
                return await CheckAsync(manager);
            case "install":
                return await InstallAsync(manager, args.Skip(1).ToArray());
            case "list":
            case "ls":
                return await ListAsync(manager);
            case "launch":
            case "run":
                return await LaunchAsync(manager, args.Skip(1).ToArray());
            case "uninstall":
            case "remove":
                return await UninstallAsync(manager, args.Skip(1).ToArray());
            case "info":
                return await InfoAsync(manager, args.Skip(1).ToArray());
            case "set-exe":
                return await SetExeAsync(manager, args.Skip(1).ToArray());
            case "set-args":
                return await SetArgsAsync(manager, args.Skip(1).ToArray());
            case "set-note":
                return await SetNoteAsync(manager, args.Skip(1).ToArray());
            case "help":
            case "--help":
            case "-h":
                PrintHelp();
                return 0;
            default:
                Console.Error.WriteLine($"Unknown command: '{command}'");
                PrintHelp();
                return 1;
        }
    }

    private static async Task<int> CheckAsync(AppManager manager)
    {
        Console.WriteLine("Checking system compatibility...");
        var result = await manager.CheckCompatibilityAsync();
        Console.WriteLine(result.WineInstalled ? "Wine: FOUND" : "Wine: NOT FOUND");
        foreach (var m in result.Messages)
        {
            Console.WriteLine("  - " + m);
        }
        var wine = result.Runtime.Wine;
        if (wine?.IsAvailable == true)
        {
            Console.WriteLine($"  Version: {wine.Version ?? "unknown"}");
            Console.WriteLine($"  Path: {wine.WinePath}");
            Console.WriteLine($"  64-bit: {(wine.Supports64Bit ? "yes" : "no")}");
            Console.WriteLine($"  32-bit: {(wine.Supports32Bit ? "yes" : "no")}");
        }
        return 0;
    }

    private static async Task<int> InstallAsync(AppManager manager, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: winrunner install <name> <installer.exe|installer.msi> [--exe <path>] [--arch win64|win32] [--arg <argument>]");
            return 1;
        }

        var name = args[0];
        var installer = args[1];
        string? exePath = null;
        var arch = "win64";
        var extraArgs = new System.Collections.Generic.List<string>();

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exe" when i + 1 < args.Length: exePath = args[++i]; break;
                case "--arch" when i + 1 < args.Length: arch = args[++i]; break;
                case "--arg" when i + 1 < args.Length:
                    extraArgs = extraArgs.Append(args[++i]).ToList();
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}");
                    return 1;
            }
        }

        Console.WriteLine($"Installing '{name}' from {installer} (arch: {arch})...");
        var progress = new Progress<string>(msg => Console.WriteLine("  " + msg));
        var result = await manager.InstallAppAsync(name, installer, exePath, arch,
            installerArguments: extraArgs, progress: progress);

        if (!result.Success)
        {
            Console.Error.WriteLine("Installation failed: " + result.ErrorMessage);
            return 1;
        }

        Console.WriteLine("Installation completed.");
        if (result.AppId != null)
        {
            Console.WriteLine($"App id: {result.AppId}");
            Console.WriteLine($"Launch with: winrunner launch {result.AppId}");
        }
        return 0;
    }

    private static async Task<int> ListAsync(AppManager manager)
    {
        var apps = await manager.ListAppsAsync();
        if (apps.Count == 0)
        {
            Console.WriteLine("No apps installed. Use 'winrunner install <name> <installer>' to add one.");
            return 0;
        }

        Console.WriteLine($"{"ID",-28} {"NAME",-30} {"ARCH",-7} {"LAUNCHES",-9} LAST LAUNCHED");
        Console.WriteLine(new string('-', 90));
        foreach (var a in apps)
        {
            var last = a.LastLaunchedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
            Console.WriteLine($"{a.Id,-28} {Truncate(a.Name, 30),-30} {a.Architecture,-7} {a.LaunchCount,-9} {last}");
        }
        Console.WriteLine(new string('-', 90));
        Console.WriteLine($"{apps.Count} app(s) installed.");
        return 0;
    }

    private static async Task<int> LaunchAsync(AppManager manager, string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: winrunner launch <id> [--wait]");
            return 1;
        }
        var id = args[0];
        var wait = args.Contains("--wait");
        var result = await manager.LaunchAppAsync(id, wait);
        if (!result.Success)
        {
            Console.Error.WriteLine("Failed to launch: " + result.ErrorMessage);
            return 1;
        }
        Console.WriteLine(result.LaunchedDetached
            ? $"Launched '{id}'."
            : $"Launched '{id}' (process exited with code 0).");
        return 0;
    }

    private static async Task<int> UninstallAsync(AppManager manager, string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: winrunner uninstall <id>");
            return 1;
        }
        var id = args[0];
        var removed = await manager.UninstallAppAsync(id);
        if (!removed)
        {
            Console.Error.WriteLine($"No app found with id '{id}'.");
            return 1;
        }
        Console.WriteLine($"Uninstalled '{id}'.");
        return 0;
    }

    private static async Task<int> InfoAsync(AppManager manager, string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: winrunner info <id>");
            return 1;
        }
        var app = await manager.GetAppAsync(args[0]);
        if (app == null)
        {
            Console.Error.WriteLine($"No app found with id '{args[0]}'.");
            return 1;
        }
        Console.WriteLine($"ID:        {app.Id}");
        Console.WriteLine($"Name:      {app.Name}");
        Console.WriteLine($"Arch:      {app.Architecture}");
        Console.WriteLine($"Installer: {app.InstallerPath ?? "-"}");
        Console.WriteLine($"Executable:{app.ExecutablePath ?? "-"}");
        Console.WriteLine($"Arguments: {app.LaunchArguments ?? "-"}");
        Console.WriteLine($"Installed: {app.InstalledAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Last run:  {app.LastLaunchedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-"}");
        Console.WriteLine($"Launches:  {app.LaunchCount}");
        Console.WriteLine($"Notes:     {app.Notes ?? "-"}");
        Console.WriteLine($"Prefix:    {manager.Prefixes.GetPrefixDirectory(app.Id)}");
        return 0;
    }

    private static async Task<int> SetExeAsync(AppManager manager, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: winrunner set-exe <id> <path>");
            return 1;
        }
        var ok = await manager.UpdateAppAsync(args[0], a => a.ExecutablePath = args[1]);
        Console.WriteLine(ok ? $"Set executable for '{args[0]}'." : $"No app found with id '{args[0]}'.");
        return ok ? 0 : 1;
    }

    private static async Task<int> SetArgsAsync(AppManager manager, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: winrunner set-args <id> <arguments>");
            return 1;
        }
        var ok = await manager.UpdateAppAsync(args[0], a => a.LaunchArguments = args[1]);
        Console.WriteLine(ok ? $"Set launch arguments for '{args[0]}'." : $"No app found with id '{args[0]}'.");
        return ok ? 0 : 1;
    }

    private static async Task<int> SetNoteAsync(AppManager manager, string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: winrunner set-note <id> <note>");
            return 1;
        }
        var ok = await manager.UpdateAppAsync(args[0], a => a.Notes = args[1]);
        Console.WriteLine(ok ? $"Set note for '{args[0]}'." : $"No app found with id '{args[0]}'.");
        return ok ? 0 : 1;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s[..(max - 3)] + "...";
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            WinRunner — run Windows software on Linux/macOS via Wine

            USAGE:
              winrunner <command> [options]

            COMMANDS:
              check                    Detect Wine and report compatibility
              install <name> <file>    Install an .exe or .msi into an isolated prefix
                  [--exe <path>]       Main executable to launch (relative to drive_c)
                  [--arch win64|win32] Prefix architecture (default win64)
                  [--arg <arg>]        Extra argument passed to the installer
              list                     List installed apps
              launch <id> [--wait]     Launch an installed app
              uninstall <id>           Remove an app and its prefix
              info <id>                Show detailed info about an app
              set-exe <id> <path>      Set the main executable for an app
              set-args <id> <args>     Set launch arguments for an app
              set-note <id> <note>     Attach a note to an app
              help                     Show this help

            EXAMPLES:
              winrunner check
              winrunner install "7-Zip" ~/Downloads/7z2409-x64.exe
              winrunner install "Office" setup.msi --arch win32
              winrunner launch 7-zip
              winrunner uninstall 7-zip
            """);
    }
}
