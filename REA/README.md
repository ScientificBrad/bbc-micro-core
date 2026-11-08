# REVERSE ENGINEERING & ARCHAEOLOGY (REA) INDEX

**Project:** 003 - Acorn BBC Micro Model B Reconstruction & Emulation  
**Series:** Historical Computer Reverse Engineering  
**Lead Researcher:** ScientificBrad  
**Date:** 2026-11-08  
**Repository:** `ScientificBrad/bbc-micro-core`

---

## Technical Documentation Index

1. [REA-01: ROM Inventory & Cryptographic Hashes](file:///D:/Emulation/BBCMicro/REA/01_ROM_INVENTORY.md)
   - Complete verification matrix of `os12.rom`, `basic2.rom`, `dfs09.rom`, and `os120.bin`.
   - Hardware vectors, code/data segment breakdowns, standard OS entry point jump table.

2. [REA-02: Firmware Identification & Provenance](file:///D:/Emulation/BBCMicro/REA/02_FIRMWARE_IDENTIFICATION.md)
   - Detailed historical context and technical breakdown of MOS 1.20, BBC BASIC II, and Acorn DFS 0.90.
   - Sophie Wilson's design innovations and token architecture.
   - Intel 8271 Disc Filing System mechanics and sector catalogue format.

3. [REA-03: Memory Architecture & I/O Subsystem](file:///D:/Emulation/BBCMicro/REA/03_MEMORY_ARCHITECTURE.md)
   - Comprehensive memory map from `$0000` to `$FFFF`.
   - Low memory breakdown: Zero page workspaces, 6502 stack, indirect RAM vectors (`$0200-$0235`), buffer pointers.
   - Video display memory sizing and `HIMEM` allocation across Modes 0 to 7.
   - Sideways ROM bank-switching mechanics (Banks 0 to 15, `$8000-$BFFF`).
   - Memory-mapped I/O decoding for FRED, JIM, and SHEILA (`$FE00-$FEFF`).

4. [REA-04: MOS 1.20 Reassembly Verification](file:///D:/Emulation/BBCMicro/REA/04_MOS_REASSEMBLY_VERIFICATION.md)
   - ACME cross-assembler verification report for Toby Nelson's reassembly (`os120_acme.a`).
   - Resolution of the 768-byte `$FC00-$FEFF` I/O region discrepancy between physical mask (Easter Egg credits string) and software open-bus dumps.

5. [Findings Ledger](file:///D:/Emulation/BBCMicro/REA/FINDINGS_LEDGER.md)
   - Formal audit trail classifying findings into `[INDEPENDENT]`, `[CONFIRMED-REF]`, `[LEARNED-REF]`, and `[IMPLEMENTATION]`.
