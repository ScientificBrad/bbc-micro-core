# REA-04: ACORN MOS 1.20 REASSEMBLY VERIFICATION & COMPARISON

**Project:** 003 - Acorn BBC Micro Model B Reconstruction & Emulation  
**Series:** Historical Computer Reverse Engineering  
**Researcher:** ScientificBrad  
**Date:** 2026-11-08  
**Repository:** `ScientificBrad/bbc-micro-core`

---

## 1. Reassembly Execution & Toolchain

Toby Nelson's annotated MOS 1.20 source (`os120_acme.a`) was verified using the ACME cross-assembler.

### Command Line Execution:
```powershell
tools\acme\acme.exe -o references\tobylobster\os120.bin references\tobylobster\os120_acme_compat.a
```

### Verification Result:
- Target File: `references\tobylobster\os120.bin`
- File Size: `16,384` bytes
- MD5: `0a59a5ba15fe8557b5f7fee32bbd393a` (Exact match to Toby Nelson's published hash)
- SHA-256: `2d9fea69017864f6962704481829f95fee08446c8c3a13826d5d4e44000ac9de`
- CRC32: `3C14FC70`
- Assembler Status: 0 Errors, 0 Warnings

---

## 2. Binary Comparison: `os120.bin` vs Original `os12.rom`

A complete 16,384-byte bitwise comparison was performed between the assembled image (`os120.bin`) and our baseline ROM dump (`os12.rom`).

### Comparison Statistics:
- Total Addresses Compared: 16,384 (`$C000-$FFFF`)
- Total Matching Bytes: **15,616** (95.3125%)
- Total Byte Discrepancies: **768** (4.6875%)
- Discrepancy Start Address: `$FC00` (Offset `0x3C00`)
- Discrepancy End Address: `$FEFF` (Offset `0x3EFF`)

### Range Breakdown:

| Address Range | Size | Comparison Status | Description |
| :--- | :--- | :--- | :--- |
| `$C000-$FBFF` | 15,360 bytes | **100% IDENTICAL** | Operating system code, VDU drivers, math routines, CLI, CFS |
| `$FC00-$FEFF` | 768 bytes | **I/O WINDOW** | Hardware I/O area: FRED (`$FCxx`), JIM (`$FDxx`), SHEILA (`$FExx`) |
| `$FF00-$FFFF` | 256 bytes | **100% IDENTICAL** | Extended vectors, OS jump table (`$FFCE-$FFF7`), hardware vectors |

### Detailed Discrepancy Analysis ($FC00-$FEFF):
1. **Physical ROM Mask (`os120.bin`):**
   Contains the Acorn Easter Egg tribute string:
   ```text
   (C) 1981 Acorn Computers Ltd.Thanks are due to the following contributors
   to the development of the BBC Computer (among others too numerous to mention):-
   David Allen,Bob Austin,Ram Banerjee,Paul Bond,Allen Boothroyd,Cambridge,
   Cleartone,John Coll,John Cox,Andy Cripps,Chris Curry,6502 designers,
   Jeremy Dion,Tim Dobson,Joe Dunn,Paul Farrell,Ferranti,Steve Furber,Jon Gibbons,
   Andrew Gordon,Lawrence Hardwick,Dylan Harris,Hermann Hauser,Hitachi,Andy Hopper,
   ICL,Martin Jackson,Brian Jones,Chris Jordan,David King,David Kitson,
   Paul Kriwaczek,Computer Laboratory,Peter Miller,Arthur Norman,Glyn Phillips,
   Mike Prees,John Radcliffe,Wilberforce Road,Peter Robinson,Richard Russell,
   Kim Spence-Jones,Graham Tebby,Jon Thackray,Chris Turner,Adrian Warner,
   Roger Wilson,Alan Wright.
   ```
2. **Software Memory Dump (`os12.rom`):**
   Reads `$FC00-$FDFF` as `$FF` (unconnected 1 MHz bus floating pull-ups) and `$FE00-$FEFF` as active register contents of the CRTC, Video ULA, and 6522 VIAs.

### Conclusion:
The executable logic of the operating system is 100% byte-for-byte identical. At runtime, the 6502 processor cannot read ROM from `$FC00-$FEFF` on actual hardware because the address decoder asserts I/O select signals instead of ROM chip select. Therefore, both ROM images execute identically in the emulator.
