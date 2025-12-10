```
██████╗ ██╗     ██╗██████╗ ██████╗  ██████╗  █████╗ ██████╗ ██████╗
██╔════╝██║     ██║██╔══██╗██╔══██╗██╔═══██╗██╔══██╗██╔══██╗██╔══██╗
██║     ██║     ██║██████╔╝██████╔╝██║   ██║███████║██████╔╝██║  ██║
██║     ██║     ██║██╔═══╝ ██╔══██╗██║   ██║██╔══██║██╔══██╗██║  ██║
╚██████╗███████╗██║██║     ██████╔╝╚██████╔╝██║  ██║██║  ██║██████╔╝
 ╚═════╝╚══════╝╚═╝╚═╝     ╚═════╝  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝
      ████████╗██╗   ██╗██████╗ ███████╗██████╗
      ╚══██╔══╝╚██╗ ██╔╝██╔══██╗██╔════╝██╔══██╗
         ██║    ╚████╔╝ ██████╔╝█████╗  ██████╔╝
         ██║     ╚██╔╝  ██╔═══╝ ██╔══╝  ██╔══██╗
         ██║      ██║   ██║     ███████╗██║  ██║
         ╚═╝      ╚═╝   ╚═╝     ╚══════╝╚═╝  ╚═╝
         made by WAM-Software (c) since 1997
```

**Clipboard Typer** — Windows tray tool that **types** your clipboard (no paste action) after a delay, with adjustable typing speed and a "Start with Windows" toggle.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](#)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.x-lightgrey)](#)
[![Hotkey](https://img.shields.io/badge/Hotkey-Ctrl+Alt+V-green)](#)

## Highlights
- **Ctrl+Alt+V**: types the clipboard after a default 5s delay.
- **Tray menu**: choose delay (5s/2s/0s) and typing speed (60/40/20/10 ms per char).
- **Start with Windows**: checkbox adds/removes Run key in HKCU.
- **Unicode keystrokes** via `SendInput(KEYEVENTF_UNICODE)` (emoji/accents work).
- **Deliberately slower** typing (extra micro pause) so characters don’t get dropped.

## Quickstart
1) Download or build `dist/ClipboardTyper.exe`.  
2) Launch the exe → icon appears in the system tray.  
3) Copy text → press **Ctrl+Alt+V** → after the delay it will be typed.  
4) Right-click the tray icon for delay, typing speed, or "Start with Windows".

## Build
Requires: .NET Framework 4.x (csc.exe present on Windows).

```powershell
cd clipboard-typer
./build.ps1
```

Output: `dist/ClipboardTyper.exe`.

## Why type instead of paste?
Some apps block Ctrl+V or detect paste actions. Real keystrokes bypass that. Typing speed is intentionally a bit slower for reliability.

## Tray options
- Delay: 5s / 2s / 0s
- Typing speed: 60 / 40 / 20 / 10 ms per char
- Start with Windows: on/off (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
- Exit

## License
MIT
