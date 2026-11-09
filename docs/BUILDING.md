# Building & Installation Guide

---

## 1. Prerequisites

- **SDK:** Microsoft .NET 8.0 SDK or higher (`dotnet --version` >= 8.0.0)
- **OS:** Windows 10 / Windows 11 (64-bit x64 or ARM64)
- **Python (Optional for reports & tools):** Python 3.10+ with `reportlab` and `Pillow`

---

## 2. Compilation

Clone the repository and build using the .NET CLI:

```powershell
# Restore dependencies and build solution in Release mode
dotnet build -c Release
```

The resulting binaries will be placed in:
`src/BbcMicro.Emulator/bin/Release/net8.0-windows/BbcMicro.Emulator.exe`

---

## 3. Creating a Standalone Windows Distribution

To create a single-folder standalone distribution that runs on any modern Windows system without requiring a pre-installed .NET runtime:

```powershell
dotnet publish src/BbcMicro.Emulator/BbcMicro.Emulator.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
```

---

## 4. Running the Emulator

### Interactive GUI Mode:
```powershell
# Launch default BBC Model B environment (boots into Acorn MOS 1.20 Mode 7)
dotnet run --project src/BbcMicro.Emulator -c Release -- --window

# Mount a floppy disk image (.ssd) and autoboot
dotnet run --project src/BbcMicro.Emulator -c Release -- --window --disk games/Disc002-ChuckieEgg.ssd --autoboot
```

### Controls & Hotkeys:
| Key | Function |
| :--- | :--- |
| **`ESC`** | Graceful exit from emulator |
| **`Pause`** or **`Ctrl+P`** | Pause / Resume CPU execution |
| **`F10`** | Hardware reset (re-triggers 6502 reset vector `$FFFC`) |
| **`Physical Keyboard`** | Full 1:1 mapping to BBC Micro keyboard matrix |

---

## 5. Running the Test Suite

Execute the automated xUnit unit and regression test suite:

```powershell
dotnet test tests/BbcMicro.Tests -c Release
```
Expected output:
```text
Passed!  - Failed: 0, Passed: 36, Skipped: 0, Total: 36
```
