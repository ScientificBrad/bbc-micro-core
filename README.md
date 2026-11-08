# Acorn BBC Micro Model B (1981) - Complete Firmware Reverse Engineering, Reconstruction & Emulation

**Project:** 003  
**Series:** Historical Computer Reverse Engineering  
**Lead Researcher:** ScientificBrad  
**Repository:** `ScientificBrad/bbc-micro-core`  
**Target Application:** *Granny's Garden* (1983, 4Mation)

---

## 1. Overview

**Project 003** is an independent, cycle-accurate reconstruction of the **Acorn BBC Microcomputer Model B (1981)**, executing genuine Acorn MOS 1.20, BBC BASIC II, and Acorn DFS 0.90 firmware.

Following the methodologies established in **Project 001** (*Commodore 4064 Core*) and **Project 002** (*Commodore 128 Dual-CPU Core*), this project combines rigorous reverse engineering archaeology (REA), firmware verification, and an ultra-fast, standalone C# (.NET 8) emulator engine.

---

## 2. Firmware Inventory & Verification

All baseline firmware files reside in `roms/`:

| ROM | Subsystem | Mapped Range | Size (Bytes) | MD5 | CRC32 | SHA-256 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `os12.rom` | Acorn MOS 1.20 | `$C000-$FFFF` | 16,384 | `3f724370ece4e4b790f26a8eca196e89` | `A4BFAEB9` | `b0ad5c0b2e7d5776cc65d643989c02c66f7823df1f5c1c528833588c5a3e7a07` |
| `basic2.rom` | BBC BASIC II | `$8000-$BFFF` (Slot 15) | 16,384 | `2cc67be4624df4dc66617742571a8e3d` | `79434781` | `45bd55dc0f6f0f8f1fe9e2481de7def206565eec8f600ba3068b849ca4132079` |
| `dfs09.rom` | Acorn DFS 0.90 | `$8000-$9FFF` (Slot 14) | 8,192 | `803530149c274994fabc6d28cbe03679` | `3CE609CF` | `ea8230cf0e93bd874df81d12f797a2cbd5038dca6b3cdd05d4193673086f9a6c` |
| `os120.bin` (Reassembled) | MOS 1.20 (With Easter Egg) | `$C000-$FFFF` | 16,384 | `0a59a5ba15fe8557b5f7fee32bbd393a` | `3C14FC70` | `2d9fea69017864f6962704481829f95fee08446c8c3a13826d5d4e44000ac9de` |

### Key Firmware Discovery:
The 768-byte discrepancy between Toby Nelson's reassembled `os120.bin` and the runtime `os12.rom` dump was isolated exclusively to `$FC00-$FEFF` (I/O region FRED, JIM, and SHEILA). Outside this region, all **15,616 bytes are 100% byte-for-byte identical**. Physical ROM masks contain the famous 47-contributor Acorn Easter Egg tribute here, while live hardware software dumps return peripheral register/open bus float (`$FF`).

---

## 3. Emulator Architecture

Built in high-performance C# (.NET 8):
- **CPU (`Cpu6502.cs`):** MOS 6502 core with all official opcodes, BCD decimal mode (`ADC`/`SBC`), interrupt masking, and cycle accuracy.
- **Memory Bus (`BbcBus.cs`):** Full 64 KB address decoding: 32 KB RAM, 16 KB 16-slot sideways ROM banking (`$8000-$BFFF`), and MOS 1.20 (`$C000-$FFFF`).
- **SHEILA Subsystem:**
  - Motorola 6845 CRTC video controller.
  - Video ULA and palette registers.
  - System 6522 VIA: 100Hz Timer 1, 50Hz VSync interrupts, address latch IC32, and keyboard matrix scanner with CA2 column sense lines.
  - User 6522 VIA: Parallel printer and user port registers.
  - ROM select latch at `$FE30`.
- **Mode 7 Teletext Renderer (`TeletextRenderer.cs`):** Text buffer rendering from `$7C00-$7FFF`.

---

## 4. Building and Running

### Prerequisites:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Standalone Emulator:
```powershell
# Interactive / Visual Boot:
dotnet run --project src/BbcMicro.Emulator/BbcMicro.Emulator.csproj

# Headless Test with Command Injection:
dotnet run --project src/BbcMicro.Emulator/BbcMicro.Emulator.csproj -- --test --exec "PRINT 40+2"

# Star Command Test:
dotnet run --project src/BbcMicro.Emulator/BbcMicro.Emulator.csproj -- --test --exec "*HELP"
```

### Run Automated Test Suite:
```powershell
dotnet test tests/BbcMicro.Tests/BbcMicro.Tests.csproj
```

---

## 5. Documentation Structure

- [REA-01: ROM Inventory](file:///D:/Emulation/BBCMicro/REA/01_ROM_INVENTORY.md)
- [REA-02: Firmware Identification](file:///D:/Emulation/BBCMicro/REA/02_FIRMWARE_IDENTIFICATION.md)
- [REA-03: Memory Architecture](file:///D:/Emulation/BBCMicro/REA/03_MEMORY_ARCHITECTURE.md)
- [REA-04: MOS 1.20 Reassembly Verification](file:///D:/Emulation/BBCMicro/REA/04_MOS_REASSEMBLY_VERIFICATION.md)
- [REA Findings Ledger](file:///D:/Emulation/BBCMicro/REA/FINDINGS_LEDGER.md)
- [Toby Nelson Reference Archive](file:///D:/Emulation/BBCMicro/references/tobylobster/README.md)
