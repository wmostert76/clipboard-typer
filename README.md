```
   _____ _ _ _ _           _               _       _______          _
  / ____(_) | (_)         | |             | |     |__   __|        | |
 | |     _| | |_ _ __   __| | ___ _ __ ___| |_ ___   | | ___   ___ | |__
 | |    | | | | | '_ \ / _` |/ _ \ '__/ __| __/ _ \  | |/ _ \ / _ \| '_ \
 | |____| | | | | | | | (_| |  __/ |  \__ \ ||  __/  | | (_) | (_) | |_) |
  \_____|_|_|_|_|_| |_|\__,_|\___|_|  |___/\__\___|  |_|\___/ \___/|_.__/
```

**Clipboard Typer** — Windows traytool dat je clipboard-inhoud **intypt** (geen plak-actie) na een vertraging, met instelbare typesnelheid en “Start met Windows”-toggle.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](#)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.x-lightgrey)](#)
[![Hotkey](https://img.shields.io/badge/Hotkey-Ctrl+Shift+V-green)](#)

## Highlights
- **Ctrl+Shift+V**: typt de clipboard na standaard 5s.
- **Tray-menu**: kies delay (5s/2s/0s) en typesnelheid (60/40/20/10 ms/teken).
- **Start met Windows**: vinkje zet/haalt Run-key in HKCU.
- **Unicode toetsaanslagen** via `SendInput(KEYEVENTF_UNICODE)` (emoji/accenten werken).
- **Langzamer, betrouwbaarder** typen (extra mini-pauze) zodat tekens niet wegvallen.

## Snel starten
1) Download of build `dist/ClipboardTyper.exe`.  
2) Start het exe → icoon in de system tray.  
3) Kopieer tekst → druk **Ctrl+Shift+V** → na de delay wordt het ingetikt.  
4) Rechtsklik tray-icoon om delay, typesnelheid of “Start met Windows” te kiezen.

## Build
Vereist: .NET Framework 4.x (csc.exe aanwezig op Windows).

```powershell
cd clipboard-typer
./build.ps1
```

Uitvoer: `dist/ClipboardTyper.exe`.

## Waarom typen i.p.v. plakken?
Sommige apps blokkeren Ctrl+V of detecteren plakken. Door echte toetsaanslagen te sturen, omzeil je dat. Typesnelheid is bewust rustiger voor betrouwbaarheid.

## Tray-opties
- Delay: 5s / 2s / 0s
- Typesnelheid: 60 / 40 / 20 / 10 ms per teken
- Start met Windows: aan/uit (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
- Exit

## Licentie
MIT
