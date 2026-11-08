# CHANGELOG - PROJECT 003: BBC MICRO MODEL B

All notable milestones and technical deliverables for Project 003 are documented here.

## [0.1.0] - 2026-11-08 - Phase 1, Phase 2 & Core Boot Milestone

### Reverse Engineering Archaeology (REA)
- **Phase 1 Complete:** Catalogued and verified baseline ROM set (`os12.rom`, `basic2.rom`, `dfs09.rom`) with MD5, CRC32, and SHA-256 hashes.
- **Phase 2 Complete:** Acquired Toby Nelson's annotated MOS 1.20 reassembly (`os120_acme.a`) into `references/tobylobster/` with full provenance, chapter guide, and licensing attribution.
- **Phase 3 Complete:** Assembled `os120.bin` using ACME and verified match with Toby Nelson's published checksum (`0a59a5ba15fe8557b5f7fee32bbd393a`).
- **Discrepancy Resolution:** Discovered and proved that the 768-byte variance between `os120.bin` and `os12.rom` is restricted to `$FC00-$FEFF` (I/O FRED/JIM/SHEILA open bus vs ROM mask Easter Egg credits string). Outside this region, all 15,616 bytes are 100% identical.
- Created `REA/01_ROM_INVENTORY.md`, `REA/02_FIRMWARE_IDENTIFICATION.md`, `REA/03_MEMORY_ARCHITECTURE.md`, `REA/04_MOS_REASSEMBLY_VERIFICATION.md`, and `REA/FINDINGS_LEDGER.md`.

### Emulator Implementation
- Created modular, high-performance C# (.NET 8) emulator in `src/BbcMicro.Core` and `src/BbcMicro.Emulator`.
- Implemented cycle-accurate MOS 6502 CPU (`Cpu6502.cs`) with BCD decimal mode, interrupts, and stack.
- Implemented full 64 KB BBC Model B memory map with 16-slot sideways ROM bank switching (`$8000-$BFFF`).
- Implemented SHEILA I/O subsystems:
  - System 6522 VIA: 100Hz Timer 1, 50Hz VSync interrupts, address latch IC32, and keyboard matrix column scanner (CA2 trigger).
  - User 6522 VIA: Parallel printer and user ports.
  - Motorola 6845 CRTC video controller registers.
  - Video ULA and palette registers.
  - ROM Select Latch at `$FE30`.
- Implemented Mode 7 Teletext screen rendering (`TeletextRenderer.cs`) from `$7C00-$7FFF`.
- Successfully executed genuine Acorn MOS 1.20 cold boot into BBC BASIC II, displaying the classic banner:
  `BBC Computer 32K`
  `Acorn DFS`
  `BASIC`
  `>`
- Implemented direct keyboard buffer injection for immediate command execution (`PRINT`, loops, `*HELP`).
- Verified OSHWM dynamic memory claim with DFS (`PAGE = &1900`, `HIMEM = &7C00`).
- Implemented xUnit automated test suite with 7 passing integration tests.
