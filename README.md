# LockMyScreen

A tiny Windows timer that **locks your computer** (or just reminds you) when time is up.

Put the soup on the stove, start a 30-minute timer, keep working. When the time runs out, Windows locks the screen, just like pressing **Win+L**, so you can't miss it.

## Features

- **Two trigger modes**
  - *after* a set duration (hours / minutes / seconds), with quick presets: 5 min, 15 min, 30 min, 1 h
  - *at* a specific time of day (if that time has already passed today, it fires tomorrow)
- **Two actions**
  - **lock** the computer (standard Windows lock screen)
  - **remind only**: a large always-on-top window that keeps beeping until you close it. There is no snooze button, on purpose.
- **Repeat**
  - in *after* mode, the next countdown starts only once you unlock the PC or close the reminder, so time spent away from the desk doesn't count
  - in *at* mode, it fires every day at the same time
- **Desktop widget**: a compact always-on-top bar with a minutes field and a Start button that turns into a live countdown once started. You can drag it anywhere, set its transparency (it turns fully opaque on hover) or hide it.
- **Runs in the system tray**: starts hidden, and the main window opens from the tray icon.
- **Languages**: English, Russian, Ukrainian. Chosen from the Windows display language on first run, switchable at any time from the tray menu.
- **Remembers settings** between runs.
- **Single file, no dependencies**: built on .NET Framework 4.8, which ships with Windows 10 and 11.

## Requirements

Windows 10 or Windows 11. Nothing else needs to be installed.

## Installation

Download `LockMyScreen-Setup-<version>.exe` from the [Releases](https://github.com/BokhanMax/lockmyscreen/releases) page and run it.

- Installs for the current user only (`%LocalAppData%\Programs\LockMyScreen`), so no administrator rights are required.
- Options: start with Windows, desktop shortcut.
- Upgrades install over the previous version and keep your settings.
- To uninstall, go to **Settings → Apps**. The uninstaller asks whether to delete your settings too.

> **Windows SmartScreen warning.** The installer is not code-signed, so on first run Windows may show *“Windows protected your PC”*. Click **More info → Run anyway**.

You can also skip the installer and just run `LockMyScreen.exe` directly. It is portable.

## Usage

| Where | What you can do |
| --- | --- |
| **Widget** | Enter minutes, press **Start** (or Enter). Press **Stop** to cancel. |
| **Tray icon**, left click | Open the main window: mode, action, repeat, pause/resume |
| **Tray icon or widget**, right click | Open, show/hide widget, widget transparency, language, exit |

Pressing **Start** in the main window minimises it to the tray. Launching the program a second time opens the main window of the instance that is already running.

Settings are stored in `%AppData%\LockMyScreen\settings.txt`.

## Building from source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 8 or newer. It is used only as the build tool: the app itself targets .NET Framework 4.8, and the reference assemblies are downloaded from NuGet automatically.
- [Inno Setup 6](https://jrsoftware.org/isinfo.php), needed only to build the installer:

  ```powershell
  winget install JRSoftware.InnoSetup
  ```

### Build the app

```powershell
dotnet build -c Release
```

Output: `bin\Release\net48\LockMyScreen.exe`

### Build the installer

```powershell
powershell -ExecutionPolicy Bypass -File build-installer.ps1
```

Output: `dist\LockMyScreen-Setup-<version>.exe`

The version comes from `<Version>` in `LockMyScreen.csproj`.

## Project structure

| File | Purpose |
| --- | --- |
| `Program.cs` | Entry point, single-instance guard, `--exit` command used by the installer |
| `MainForm.cs` | Main window, timer logic, tray icon and menu, settings load/save |
| `WidgetForm.cs` | Desktop widget |
| `ReminderForm.cs` | “Time's up” reminder window |
| `Strings.cs` | All UI text in every supported language |
| `Settings.cs` | Simple `key=value` settings file |
| `Polyfills.cs` | Compiler support types that let modern C# features build on .NET Framework 4.8 |
| `app.manifest` | Windows 10/11 compatibility, DPI awareness |
| `installer/LockMyScreen.iss` | Inno Setup script |
| `build-installer.ps1` | Builds the app and the installer in one step |
| `icons/` | Application icon (`app.ico` is embedded into the exe) |

## Adding a language

1. Add a value to the `Language` enum in `Strings.cs`.
2. Add a `Strings` instance with all the texts, and map it in `Strings.For()`. Every property is `required`, so the compiler reports any missing translation.
3. Optionally map the Windows language code in `Strings.Initial()` and add the language to the installer's `[Languages]` and `[CustomMessages]` sections.

## Command line

| Argument | Effect |
| --- | --- |
| *(none)* | Start the app, or open the main window if it is already running |
| `--exit` | Ask the running instance to close (used by the installer during upgrade and uninstall) |

## License

[MIT](LICENSE) © 2026 Max Bokhan
