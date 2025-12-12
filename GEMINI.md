# ClipboardTyper Project Context

## Project Overview

**ClipboardTyper** is a lightweight Windows tray utility written in C# (.NET Framework 4.x). Its primary function is to "type" the contents of the system clipboard as a sequence of keystrokes, rather than using the standard paste command. This is useful for bypassing paste restrictions in certain applications (e.g., remote desktop sessions, legacy forms).

**Key Features:**
*   **Global Hotkey:** Triggers typing via `Ctrl+Shift+V`.
*   **Simulated Input:** Uses the Win32 `SendInput` API with `KEYEVENTF_UNICODE` to support a wide range of characters.
*   **Tray Interface:** Provides options for delay (0s, 2s, 5s), typing speed, and history access via a system tray icon.
*   **Startup Integration:** Optional "Start with Windows" feature via the Windows Registry.

## Architecture & Technology

*   **Language:** C# 4.0+
*   **Framework:** .NET Framework 4.x (Standard Windows libraries)
*   **Type:** Windows Forms Application (Hidden main form, Tray-centric)
*   **Core Files:**
    *   `src/ClipboardTyper.cs`: The entire application logic (entry point, UI, hotkey handling, input simulation) resides here.
    *   `build.ps1`: PowerShell script to compile the application using the native C# compiler (`csc.exe`).

## Building and Running

### Prerequisites
*   Windows OS
*   .NET Framework 4.x installed (Standard on modern Windows).

### Build Command
The project avoids complex build systems (like MSBuild or Visual Studio solutions) in favor of a direct compilation script.

Run the following command in PowerShell:
```powershell
./build.ps1
```
This script locates `csc.exe` and compiles `src/ClipboardTyper.cs` into `dist/ClipboardTyper.exe`.

### Running the Application
After building, the executable is located in the `dist` folder:
```powershell
./dist/ClipboardTyper.exe
```
*Note: The application starts minimized to the system tray. Look for the "i" icon.*

### CI/CD
The project uses GitHub Actions (`.github/workflows/build.yml`) to automatically build the application on `windows-latest` for every push and pull request.

## Development Conventions

*   **Single-File Structure:** All code is currently contained within `src/ClipboardTyper.cs`. When modifying, keep related classes (like `TrayApp` and the P/Invoke definitions) organized within this file unless the complexity necessitates a split.
*   **P/Invoke:** Native Windows API calls (`user32.dll`) are used for hotkey management and input simulation. Ensure correct signatures are maintained when editing `SendInput` or `RegisterHotKey` definitions.
*   **Async/Await:** The hotkey handler uses `async/await` to manage the typing delay without freezing the UI thread.
*   **Code Style:** Standard C# naming conventions.
*   **Resource Management:** The `NotifyIcon` and other GDI resources are explicitly disposed in `OnFormClosed`.

## Key Logic

*   **Typing:** Implemented in `TypeUnicode` method. It iterates over the clipboard text and sends `KEYEVENTF_UNICODE` inputs. A micro-pause (`Thread.Sleep(5)`) is added between keystrokes to ensure reliability.
*   **Persistence:** The "Start with Windows" setting directly modifies `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
