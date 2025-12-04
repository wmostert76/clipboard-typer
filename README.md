# Clipboard Typer

Klein Windows-traytool dat je clipboard **intypt** (geen plak-actie) met een vertraging en instelbare typesnelheid. Handig voor apps die plakken blokkeren of waar je echte toetsaanslagen nodig hebt.

## Features
- Ctrl+Shift+V: typt de huidige clipboardinhoud na standaard 5 seconden.
- Tray-menu: kies delay (5s/2s/0s) en typesnelheid (60/40/20/10 ms per teken).
- Start met Windows: toggle via vinkje (Run-key in HKCU).
- Unicode typing via `SendInput(KEYEVENTF_UNICODE)` zodat emoji/accenten werken.

## Installatie & gebruik
1. Download of bouw `ClipboardTyper.exe` (zie Build).
2. Start het exe; icoon verschijnt in de system tray.
3. Kopieer tekst → druk **Ctrl+Shift+V** → na de ingestelde delay wordt de tekst ingetikt.
4. Rechtsklik op het tray-icoon voor delay, typesnelheid of “Start met Windows”.

## Build
Vereist .NET Framework 4.x (csc.exe aanwezig op Windows).

```powershell
cd clipboard-typer
./build.ps1
```

Uitvoer komt in `dist/ClipboardTyper.exe`.

## Waarom geen plakken?
Sommige applicaties detecteren Ctrl+V of blokkeren plakken; echte toetsaanslagen omzeilen dat. De typesnelheid is bewust wat trager voor betrouwbaarheid.

## Licentie
MIT
