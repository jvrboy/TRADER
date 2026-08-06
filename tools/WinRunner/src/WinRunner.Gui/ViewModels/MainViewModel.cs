using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinRunner.Core;
using WinRunner.Core.Models;

namespace WinRunner.Gui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly AppManager _manager;

    public MainViewModel()
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".winrunner");
        _manager = new AppManager(dataDir);
        _ = LoadAsync();
    }

    public ObservableCollection<AppItemViewModel> Apps { get; } = new();

    /// <summary>Wine prefix architectures offered in the install form.</summary>
    public string[] Architectures { get; } = { "win64", "win32" };

    [ObservableProperty]
    private string _statusMessage = "Loading...";

    [ObservableProperty]
    private string _wineStatus = "Checking Wine...";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private AppItemViewModel? _selectedApp;

    [ObservableProperty]
    private string _installName = string.Empty;

    [ObservableProperty]
    private string _installerPath = string.Empty;

    [ObservableProperty]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    private string _architecture = "win64";

    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var apps = await _manager.ListAppsAsync();
            Apps.Clear();
            foreach (var a in apps)
            {
                Apps.Add(new AppItemViewModel(a));
            }

            var check = await _manager.CheckCompatibilityAsync();
            WineStatus = check.WineInstalled
                ? $"Wine ready ({check.Runtime.Wine?.Version ?? "unknown"})"
                : "Wine not detected — install Wine to run apps";

            StatusMessage = Apps.Count == 0
                ? "No apps installed yet. Use Install to add one."
                : $"{Apps.Count} app(s) installed.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task BrowseInstallerAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel == null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Windows installer (.exe or .msi)",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Windows installers") { Patterns = new[] { "*.exe", "*.msi" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } },
            },
        });
        if (files.Count > 0)
        {
            InstallerPath = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
            // Auto-fill the name from the filename if empty.
            if (string.IsNullOrWhiteSpace(InstallName))
            {
                InstallName = Path.GetFileNameWithoutExtension(InstallerPath);
            }
        }
    }

    private static Avalonia.Controls.Window? GetTopLevel() =>
        Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime l
            ? l.MainWindow
            : null;

    [RelayCommand]
    private async Task InstallAsync()
    {
        if (string.IsNullOrWhiteSpace(InstallName) || string.IsNullOrWhiteSpace(InstallerPath))
        {
            StatusMessage = "Enter an app name and choose an installer file.";
            return;
        }
        if (!File.Exists(InstallerPath))
        {
            StatusMessage = "Installer file not found: " + InstallerPath;
            return;
        }

        IsBusy = true;
        StatusMessage = $"Installing '{InstallName}'...";
        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var result = await _manager.InstallAppAsync(
                InstallName, InstallerPath,
                string.IsNullOrWhiteSpace(ExecutablePath) ? null : ExecutablePath,
                Architecture,
                progress: progress);

            if (result.Success)
            {
                StatusMessage = $"Installed '{InstallName}'. App id: {result.AppId}";
                InstallName = string.Empty;
                InstallerPath = string.Empty;
                ExecutablePath = string.Empty;
                await LoadAsync();
            }
            else
            {
                StatusMessage = "Install failed: " + result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Install error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (SelectedApp == null)
        {
            StatusMessage = "Select an app to launch.";
            return;
        }
        IsBusy = true;
        StatusMessage = $"Launching '{SelectedApp.Name}'...";
        try
        {
            var result = await _manager.LaunchAppAsync(SelectedApp.Id);
            StatusMessage = result.Success
                ? $"Launched '{SelectedApp.Name}'."
                : $"Failed to launch: {result.ErrorMessage}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Launch error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        if (SelectedApp == null)
        {
            StatusMessage = "Select an app to uninstall.";
            return;
        }
        IsBusy = true;
        try
        {
            var removed = await _manager.UninstallAppAsync(SelectedApp.Id);
            StatusMessage = removed
                ? $"Uninstalled '{SelectedApp.Name}'."
                : $"Could not uninstall '{SelectedApp.Name}'.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Uninstall error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SetExecutableAsync()
    {
        if (SelectedApp == null) return;
        var topLevel = GetTopLevel();
        if (topLevel == null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select the main executable",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Executables") { Patterns = new[] { "*.exe" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } },
            },
        });
        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
            var ok = await _manager.UpdateAppAsync(SelectedApp.Id, a => a.ExecutablePath = path);
            StatusMessage = ok ? $"Set executable for '{SelectedApp.Name}'." : "Update failed.";
            await LoadAsync();
        }
    }
}

/// <summary>Thin wrapper exposing an InstalledApp to the UI.</summary>
public partial class AppItemViewModel : ViewModelBase
{
    public AppItemViewModel(InstalledApp app)
    {
        Id = app.Id;
        Name = app.Name;
        Architecture = app.Architecture;
        LaunchCount = app.LaunchCount;
        LastLaunched = app.LastLaunchedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "never";
        Executable = app.ExecutablePath ?? "-";
    }

    public string Id { get; }
    public string Name { get; }
    public string Architecture { get; }
    public long LaunchCount { get; }
    public string LastLaunched { get; }
    public string Executable { get; }

    public override string ToString() => Name;
}
