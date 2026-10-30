# OpenWidget

OpenWidget is a standalone desktop system and information widget for Windows 10 and Windows 11. It combines system telemetry, weather, clipboard history, media controls, financial data, and hardware inventory in a modern acrylic interface.

[Türkçe README](README.tr.md) · [Releases](https://github.com/alztrk/OpenWidget/releases)

---

## Features

- **Desktop integration:** Runs as a borderless desktop widget without a taskbar button or focus stealing.
- **Global hotkey:** Press `Ctrl + Shift + W` to show or hide the widget.
- **Multi-monitor and DPI support:** Detects the active monitor and keeps the widget within the work area.
- **Single-instance protection:** Prevents multiple copies from running at the same time.
- **Responsive layout:** Adjusts its maximum height to the available display area and enables scrolling when needed.
- **Compact HUD mode:** Switches to a compact single-line view.
- **Game mode:** Reduces background telemetry activity when a full-screen application or game is detected.
- **Persistent configuration:** Synchronizes settings between `%APPDATA%\OpenWidget` and the portable application directory.

## Included Modules

1. **Header:** Clock, date, uptime, and game-mode status.
2. **Weather:** Current conditions and a three-day forecast through Open-Meteo.
3. **Telemetry:** CPU, GPU, VRAM, RAM, disk, and network metrics.
4. **Finance:** Currency, gold, and cryptocurrency prices with local caching.
5. **Media:** Windows Media Session controls, session switching, album artwork, and application-specific volume control.
6. **Clipboard:** Persistent text, image thumbnail, and copied-file history.
7. **Hardware:** Hardware detection and tracked reference prices.
8. **Quick actions:** Shortcuts for Windows tools, persistent scratchpad, and volume mixer access.

## Requirements

- Windows 10 version 2004 (build 19041) or later, or Windows 11
- Windows x64
- No separate .NET runtime is required when using the portable release

## Run the Release

1. Download the latest `OpenWidget-windows-x64.zip` from the [GitHub Releases](https://github.com/alztrk/OpenWidget/releases) page.
2. Extract the ZIP archive to a folder.
3. Run `OpenWidget.exe`. Installation is not required.
4. The widget appears in the upper-right area of the desktop. Use `Ctrl + Shift + W` to toggle its visibility.
5. Right-click the widget to configure modules, position, theme, auto-start, and desktop behavior.

Windows SmartScreen may show a warning because the executable is not signed with a publisher certificate. Only download releases from the official [Releases](https://github.com/alztrk/OpenWidget/releases) page.

## Run from Source

With the .NET 10 SDK installed on Windows:

```powershell
dotnet run --project OpenWidget.csproj
```

To create a self-contained portable executable:

```powershell
powershell -ExecutionPolicy Bypass -File ".\scripts\publish-portable.ps1"
```

The output is written to `dist\OpenWidget.exe`.

## Configuration and Data Files

- `%APPDATA%\OpenWidget\hardware-config.json`: User preferences, enabled widgets, theme, city, and position.
- `%APPDATA%\OpenWidget\clipboard-history.json`: Persistent clipboard history.
- `%APPDATA%\OpenWidget\scratchpad.txt`: Scratchpad contents.
- `%APPDATA%\OpenWidget\crash.log`: Unexpected error log.
- `hardware-prices.json`: Tracked hardware reference prices.
- `finance-cache.json`: Cached finance data.
- `weather-cache.json`: Cached weather data.

## Project Structure

- `Views/Widgets/`: Widget views shown in the main panel.
- `Views/Windows/`: Standalone application windows.
- `Resources/Styles/`: Shared WPF styles and theme resources.
- `Models/`: Application data models.
- `Services/`: Hardware, system, network, and application services.
- `Converters/`: XAML value converters.
- `Utils/`: Shared helper classes.
- `tests/`: Unit tests.

## Build and Test

Run the unit tests:

```powershell
dotnet test OpenWidget.sln
```

Build a Release configuration:

```powershell
dotnet build OpenWidget.sln -c Release
```

## CI

GitHub Actions runs on pushes and pull requests targeting `master` or `main`. It restores dependencies, builds the solution, runs the unit tests, and publishes a self-contained Windows x64 artifact.

## License

OpenWidget is available under the [MIT License](LICENSE).
