# ⌨️ Clipboard Typer

> Windows tray tool that TYPES your clipboard instead of pasting | Bypass paste-blocking apps | Adjustable speed & delay

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
```

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](#)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.x-lightgrey)](#)
[![Hotkey](https://img.shields.io/badge/Hotkey-Ctrl+Alt+V-green)](#)

## ✨ Features

- **Global Hotkey** - `Ctrl+Alt+V` typt je clipboard na instelbare delay
- **Adjustable Delay** - Kies 5s / 2s / 0s vertraging
- **Variable Speed** - 60 / 40 / 20 / 10 ms per karakter
- **Start with Windows** - Checkbox in tray menu
- **Unicode Support** - Emoji's en accenten werken via `SendInput`
- **Reliable Typing** - Extra micro-pauze voorkomt dropped characters

## 🚀 Quick Start

1. Download of build `ClipboardTyper.exe`
2. Start de exe → icoon verschijnt in system tray
3. Kopieer tekst → druk **Ctrl+Alt+V** → tekst wordt getypt na delay
4. Rechtermuisklik op tray icoon voor instellingen

## 🤔 Waarom typen ipv plakken?

Sommige applicaties blokkeren `Ctrl+V` of detecteren paste-acties:
- Beveiligde invoervelden
- Remote desktop sessies
- Bepaalde web formulieren
- Legacy applicaties

**Echte keystrokes bypassen dit!**

## ⚙️ Tray Opties

| Optie | Keuzes |
|-------|--------|
| **Delay** | 5s / 2s / 0s |
| **Typing Speed** | 60 / 40 / 20 / 10 ms per char |
| **Start with Windows** | On / Off |

## 🔨 Build

Vereist: .NET Framework 4.x (csc.exe aanwezig op Windows)

```powershell
cd clipboard-typer
./build.ps1
```

Output: `ClipboardTyper.exe`

## 🛠️ Technologie

- C# / .NET Framework 4.x
- Win32 `SendInput` API met `KEYEVENTF_UNICODE`
- System tray applicatie met global hotkey

## 📄 License

MIT

