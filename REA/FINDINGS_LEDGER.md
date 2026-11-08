# REA FINDINGS LEDGER: PROJECT 003 (BBC MICRO MODEL B)

**Project:** 003 - Acorn BBC Micro Model B Reconstruction & Emulation  
**Series:** Historical Computer Reverse Engineering  
**Lead Researcher:** ScientificBrad  
**Date:** 2026-11-08  
**Repository:** `ScientificBrad/bbc-micro-core`

---

## 1. Methodology & Classification Standard

In adherence to Project 003 research standards, all discoveries and engineering assertions are classified into four rigorous categories:
1. **[INDEPENDENT]:** Findings derived purely through static and dynamic analysis of original firmware binaries (`os12.rom`, `basic2.rom`, `dfs09.rom`) and binary diffing.
2. **[CONFIRMED-REF]:** Findings independently identified and subsequently confirmed against Toby Nelson's MOS 1.20 reassembly and documentation.
3. **[LEARNED-REF]:** Specific historical details, naming conventions, or architectural nuances learned directly from Toby Nelson's research.
4. **[IMPLEMENTATION]:** Original emulator engine design, C# source architecture, peripheral modeling, and test suites developed for Project 003.

---

## 2. Findings Ledger

### FL-001: Baseline ROM Identification and Cryptographic Hash Ledger
- **Category:** [INDEPENDENT]
- **Date:** 2026-11-08
- **Finding:**
  The baseline ROM set in `roms/` was inspected and cryptographically verified:
  - `os12.rom`: 16,384 bytes, MD5 `3f724370ece4e4b790f26a8eca196e89`, CRC32 `A4BFAEB9`, SHA-256 `b0ad5c0b2e7d5776cc65d643989c02c66f7823df1f5c1c528833588c5a3e7a07`.
  - `basic2.rom`: 16,384 bytes, MD5 `2cc67be4624df4dc66617742571a8e3d`, CRC32 `79434781`, SHA-256 `45bd55dc0f6f0f8f1fe9e2481de7def206565eec8f600ba3068b849ca4132079`.
  - `dfs09.rom`: 8,192 bytes, MD5 `803530149c274994fabc6d28cbe03679`, CRC32 `3CE609CF`, SHA-256 `ea8230cf0e93bd874df81d12f797a2cbd5038dca6b3cdd05d4193673086f9a6c`.

---

### FL-002: Resolution of the MOS 1.20 Checksum Discrepancy ($FC00-$FEFF I/O Window)
- **Category:** [INDEPENDENT] / [CONFIRMED-REF]
- **Date:** 2026-11-08
- **Finding:**
  - Toby Nelson's documentation states that assembling `os120_acme.a` yields MD5 `0a59a5ba15fe8557b5f7fee32bbd393a`.
  - Binary diffing `os120.bin` against `os12.rom` revealed exactly 768 byte discrepancies located strictly between `$FC00` and `$FEFF` (offsets `0x3C00` to `0x3EFF`).
  - Outside of this 768-byte window, **all 15,616 bytes are 100% byte-for-byte identical**.
  - **Explanation:** In the BBC Micro hardware architecture, `$FC00-$FEFF` is the memory-mapped I/O area (FRED, JIM, SHEILA). In the physical ROM mask, Acorn encoded an Easter Egg tribute crediting 47 contributors. However, when software dumps the ROM from a live machine, the address bus activates peripheral registers or returns open bus float (`$FF`), producing the dump hash in `os12.rom`.
  - Both images are completely valid and functionally identical at runtime because the CPU never reads ROM from `$FC00-$FEFF`.

---

### FL-003: Sideways ROM Header Structure & MOS Validation Logic
- **Category:** [INDEPENDENT] / [CONFIRMED-REF]
- **Date:** 2026-11-08
- **Finding:**
  - At cold boot (`$D9CD`), MOS 1.20 scans sideways ROM banks 15 down to 0.
  - Verification requires matching a 4-byte sequence in reverse starting at `$8000 + ($8007)` against `\0 ) C (`:
    `Offset + 0 = 0x00`, `Offset + 1 = '(' ($28)`, `Offset + 2 = 'C' ($43)`, `Offset + 3 = ')' ($29)`.
  - Both `basic2.rom` (offset `$800E`) and `dfs09.rom` (offset `$8011`) pass this validation.
  - ROM Type byte at `$8006`:
    - Bit 7: 1 = Service entries accepted (`dfs09.rom` has Bit 7=1).
    - Bit 6: 1 = Language ROM (`basic2.rom` has Bit 6=1, Bit 7=0).
    - Bits 0-3: CPU target (0 = 6502 BASIC, 2 = 6502 Machine Code).

---

### FL-004: Toby Nelson Reassembly Build Compatibility
- **Category:** [IMPLEMENTATION] / [LEARNED-REF]
- **Date:** 2026-11-08
- **Finding:**
  - Toby Nelson's source `os120_acme.a` utilized the ACME pseudo-opcode `!be16` (big-endian 16-bit word).
  - Modern standalone Windows ports of ACME required converting `!be16 expr` to ACME portable byte directives `!by >expr, <expr` (or high/low shifts).
  - With this compatibility translation (`os120_acme_compat.a`), ACME produced `os120.bin` matching MD5 `0a59a5ba15fe8557b5f7fee32bbd393a` with 0 warnings and 0 errors.

---

### FL-005: BBC BASIC II Runtime Architecture
- **Category:** [INDEPENDENT]
- **Date:** 2026-11-08
- **Finding:**
  - BASIC II enters via `$8000` with `A=1` on cold reset, jumps to `$8023`, sets up zero-page workspace `$E0-$FF`, prints the title banner, and initializes `PAGE` and `HIMEM`.
  - Communicates with OS exclusively via standard vectors (`OSWRCH`, `OSBYTE`, `OSWORD`, `OSCLI`, `OSBGET`, `OSBPUT`, `OSARGS`, `OSFILE`, `OSFIND`).
  - No direct hardware register manipulation is performed by BBC BASIC itself, ensuring clean modular separation.

---

### FL-006: Target Game Architecture: Granny's Garden (1983)
- **Category:** [INDEPENDENT]
- **Date:** 2026-11-08
- **Finding:**
  - 4Mation educational adventure game written in BBC BASIC and 6502 machine code.
  - Relies on Mode 7 Teletext graphics and sound effects, disc loading via DFS, and standard VDU control codes.
  - Emulation requirement: High-fidelity Mode 7 Teletext SAA5050 emulation, 6522 VIA system timer interrupts (100Hz), sound generator (SN76489), and Intel 8271 DFS disc image mounting (.SSD/.DSD).
