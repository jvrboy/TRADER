using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using WinRunner.Core.Configuration;
using WinRunner.Core.Installer;
using WinRunner.Core.Models;
using WinRunner.Core.Processes;
using WinRunner.Core.Wine;

namespace WinRunner.Core.Tests;

public class PrefixManagerSlugifyTests
{
    [Theory]
    [InlineData("7-Zip", "7-zip")]
    [InlineData("My Great App", "my-great-app")]
    [InlineData("  Trimmed  ", "trimmed")]
    [InlineData("Café & Bistro", "caf-bistro")]
    [InlineData("!!!", "app")]
    [InlineData("", "app")]
    [InlineData(null, "app")]
    [InlineData("App.v2 (Beta)", "app-v2-beta")]
    public void Slugify_ProducesSafeSlugs(string? input, string expected)
    {
        Assert.Equal(expected, PrefixManager.Slugify(input!));
    }
}

public class WineDetectorVersionParsingTests
{
    [Theory]
    [InlineData("wine-9.0", "wine-9.0")]
    [InlineData("wine-9.0 (Staging)", "wine-9.0")]
    [InlineData("wine-8.0.2", "wine-8.0.2")]
    [InlineData("wine-7.0-rc4", "wine-7.0")]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("not wine at all", null)]
    public void ParseVersion_ExtractsVersion(string? line, string? expected)
    {
        Assert.Equal(expected, WineDetector.ParseVersion(line));
    }
}

public class InstallerPathMappingTests
{
    [Fact]
    public void ToWindowsPath_MapsUnixRootToZDrive()
    {
        var result = InstallerService.ToWindowsPath("/home/user/setup.exe");
        Assert.Equal("Z:\\home\\user\\setup.exe", result);
    }

    [Fact]
    public void ToWindowsPath_KeepsRelativePaths()
    {
        var result = InstallerService.ToWindowsPath("C:\\Program Files\\app\\app.exe");
        Assert.Equal("C:\\Program Files\\app\\app.exe", result);
    }
}

public class SettingsManagerTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsManagerTests()
    {
        _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "winrunner-tests-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_tempDir))
        {
            System.IO.Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAndLoadApps_RoundTrips()
    {
        var settings = new SettingsManager(_tempDir);
        var apps = new List<InstalledApp>
        {
            new()
            {
                Id = "7-zip",
                Name = "7-Zip",
                InstallerPath = "/tmp/7z.exe",
                ExecutablePath = @"C:\Program Files\7-Zip\7zFM.exe",
                Architecture = "win64",
                LaunchCount = 3,
                LastLaunchedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EnvironmentVariables = new Dictionary<string, string> { ["FOO"] = "bar" },
            },
        };

        await settings.SaveAppsAsync(apps);
        var loaded = await settings.LoadAppsAsync();

        Assert.Single(loaded);
        Assert.Equal("7-zip", loaded[0].Id);
        Assert.Equal("7-Zip", loaded[0].Name);
        Assert.Equal("win64", loaded[0].Architecture);
        Assert.Equal(3, loaded[0].LaunchCount);
        Assert.Equal("bar", loaded[0].EnvironmentVariables["FOO"]);
    }

    [Fact]
    public async Task LoadApps_WhenNoFile_ReturnsEmpty()
    {
        var settings = new SettingsManager(_tempDir);
        var loaded = await settings.LoadAppsAsync();
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task LoadSettings_WhenNoFile_ReturnsDefaults()
    {
        var settings = new SettingsManager(_tempDir);
        var s = await settings.LoadSettingsAsync();
        Assert.Equal("win64", s.DefaultArchitecture);
        Assert.True(s.AutoInitializePrefix);
    }

    [Fact]
    public async Task SaveAndLoadSettings_RoundTrips()
    {
        var settings = new SettingsManager(_tempDir);
        var s = new GlobalSettings
        {
            DefaultArchitecture = "win32",
            PrefixRoot = "/custom/prefixes",
            WineBinaryOverride = "/opt/wine/bin/wine",
            AutoInitializePrefix = false,
        };
        await settings.SaveSettingsAsync(s);

        var loaded = await settings.LoadSettingsAsync();
        Assert.Equal("win32", loaded.DefaultArchitecture);
        Assert.Equal("/custom/prefixes", loaded.PrefixRoot);
        Assert.Equal("/opt/wine/bin/wine", loaded.WineBinaryOverride);
        Assert.False(loaded.AutoInitializePrefix);
    }
}

public class PrefixManagerDirectoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PrefixManager _manager;

    public PrefixManagerDirectoryTests()
    {
        _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "winrunner-prefix-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(_tempDir);
        _manager = new PrefixManager(new ProcessRunner(), _tempDir);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_tempDir))
        {
            System.IO.Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void GetPrefixDirectory_IsUnderRoot()
    {
        var dir = _manager.GetPrefixDirectory("My App");
        Assert.StartsWith(_tempDir, dir);
        Assert.EndsWith("my-app", dir);
    }

    [Fact]
    public void PrefixExists_ReturnsFalseWhenNotCreated()
    {
        Assert.False(_manager.PrefixExists("nonexistent"));
    }

    [Fact]
    public void DeletePrefix_RemovesDirectory()
    {
        var dir = _manager.GetPrefixDirectory("test-app");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "drive_c", "windows"));
        Assert.True(_manager.PrefixExists("test-app"));
        _manager.DeletePrefix("test-app");
        Assert.False(System.IO.Directory.Exists(dir));
    }

    [Fact]
    public void ListPrefixes_ReturnsCreatedPrefixes()
    {
        System.IO.Directory.CreateDirectory(_manager.GetPrefixDirectory("alpha"));
        System.IO.Directory.CreateDirectory(_manager.GetPrefixDirectory("beta"));
        var prefixes = _manager.ListPrefixes();
        Assert.Contains("alpha", prefixes);
        Assert.Contains("beta", prefixes);
    }
}
