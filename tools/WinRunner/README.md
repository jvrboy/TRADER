# WinRunner — Run Windows Software on Linux & macOS

**WinRunner** is a production-ready C# application that lets you install, manage, and run
Windows software (`.exe` and `.msi`) on Linux and macOS — by driving the mature **Wine**
compatibility layer. It ships with both a **desktop GUI** and a **command-line tool**,
sharing a single core engine.

> **What this is:** A real, working launcher/manager around Wine. It does *not*
> reimplement Windows from scratch (that's a decades-long engineering effort — Wine and
> ReactOS are proof). Instead, it makes the real Wine layer easy and safe to use: each app
> gets its own **isolated prefix**, so applications never interfere with each other.

---

## Features

- **Install `.exe` and `.msi`** into isolated per-app Wine prefixes
- **Launch** installed apps with one click (GUI) or one command (CLI)
- **Manage** apps: list, inspect, set the main executable, set launch arguments, attach notes
- **Uninstall** cleanly — removes the app's prefix and registry entry
- **Wine detection** — finds Wine on your system and reports its version and architecture support
- **Cross-platform** — runs on Linux, macOS, and Windows (built with .NET 8 + Avalonia)
- **Both GUIs and CLI** share one core engine (`WinRunner.Core`)

---

## Project structure

```
WindowsAppRunner/
├── WindowsAppRunner.sln            # Solution wiring all projects
├── src/
│   ├── WinRunner.Core/             # Core engine (shared by GUI + CLI)
│   │   ├── AppManager.cs           # High-level facade
│   │   ├── Configuration/          # Settings & app-registry persistence (JSON)
│   │   ├── Installer/              # .exe / .msi installation
│   │   ├── Launch/                 # App launching
│   │   ├── Models/                 # InstalledApp, WineInfo, etc.
│   │   ├── Processes/              # Safe process execution
│   │   └── Wine/                   # Wine detection + prefix management
│   ├── WinRunner.Cli/              # Command-line tool
│   └── WinRunner.Gui/              # Avalonia desktop GUI
└── tests/
    └── WinRunner.Core.Tests/       # xUnit tests (25 passing)
```

---

## Requirements

- **.NET 8 SDK** — to build (`dotnet build`) or run from source
- **Wine** — to actually run Windows software. Install it first:

  - **Debian/Ubuntu:** `sudo apt install wine64`
  - **Fedora:** `sudo dnf install wine`
  - **Arch:** `sudo pacman -S wine`
  - **macOS (Homebrew):** `brew install --cask wine-stable`
  - Or download from **[winehq.org](https://www.winehq.org/)**

Run `winrunner check` (CLI) or look at the status bar (GUI) to confirm Wine is detected.

---

## Building

```bash
dotnet build WindowsAppRunner.sln
dotnet test  WindowsAppRunner.sln
```

---

## Command-line usage

```
winrunner check                     # Detect Wine and report compatibility
winrunner install <name> <file>     # Install an .exe or .msi
    [--exe <path>]                  #   main executable (relative to drive_c)
    [--arch win64|win32]            #   prefix architecture (default win64)
    [--arg <arg>]                   #   extra installer argument
winrunner list                      # List installed apps
winrunner launch <id> [--wait]      # Launch an app
winrunner uninstall <id>            # Remove an app and its prefix
winrunner info <id>                 # Show app details
winrunner set-exe <id> <path>       # Set the main executable
winrunner set-args <id> <args>      # Set launch arguments
winrunner set-note <id> <note>      # Attach a note
winrunner help                      # Show help
```

### Examples

```bash
# Install 7-Zip
winrunner install "7-Zip" ~/Downloads/7z2409-x64.exe

# Install a 32-bit MSI
winrunner install "Legacy Tool" setup.msi --arch win32

# Launch it
winrunner launch 7-zip

# Point an app at its main executable
winrunner set-exe 7-zip "C:\Program Files\7-Zip\7zFM.exe"
```

---

## Desktop GUI

Launch `WinRunner.Gui` to open the desktop app:

- **Left pane** lists installed apps (name, id, architecture, last launched)
- **Right pane** installs new apps: pick a name, browse for an installer, pick the
  architecture, optionally set the main executable
- **Buttons** to launch, set the executable, and uninstall the selected app
- **Status bar** shows Wine status and current messages

---

## How it works

1. **Wine detection** (`WineDetector`) finds the `wine` binary on your PATH or in known
   locations, and probes its version and 64/32-bit support.
2. **Prefix management** (`PrefixManager`) creates an isolated Wine prefix per app under
   `~/.winrunner/prefixes/<app-id>/`. Each prefix is a full Windows environment, isolated
   from every other app.
3. **Installation** (`InstallerService`) initializes the prefix with `wineboot -i`, then runs
   the `.exe` installer, or `msiexec /i <file> /qn` for `.msi` files.
4. **Launching** (`AppLauncher`) runs the app's main executable inside its prefix with the
   correct `WINEPREFIX` and `WINEARCH` environment variables.
5. **Persistence** (`SettingsManager`) stores the app registry and global settings as JSON
   under `~/.winrunner/`.

---

## Data locations

| What                | Where                              |
|---------------------|------------------------------------|
| App registry        | `~/.winrunner/apps.json`           |
| Global settings     | `~/.winrunner/settings.json`       |
| Wine prefixes       | `~/.winrunner/prefixes/<app-id>/`  |

---

## License

Provided for your use. Wine is licensed separately under the LGPL — see
[winehq.org](https://www.winehq.org/) for details.
