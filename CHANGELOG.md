# Changelog

All notable changes to the **Acorn BBC Micro Model B Hardware Reconstruction & Emulation Core** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [v0.1.0] - 2026-11-09 — First Playable Release

### Added
- **MOS Technology 6502 CPU:** Cycle-accurate execution core supporting all official opcodes, addressing modes, stack operations, zero-page boundary wrapping, and interrupt handling (Reset, IRQ, NMI).
- **Firmware Support:** Cold boot support for genuine Acorn MOS 1.20 (`$C000-$FFFF`), BBC BASIC II in sideways Bank 15 (`$8000-$BFFF`), and Acorn DFS 0.90 in sideways Bank 14 (`$8000-$9FFF`).
- **Memory Architecture:** 32 KB contiguous base RAM (`$0000-$7FFF`), memory-mapped I/O page (`$FE00-$FEFF`), and 16-bank Sideways ROM switching via `$FE30`.
- **Intel 8271 Floppy Disk Controller:** Memory-mapped register handling (`$FE80-$FE84`), DRQ/NMI byte streaming, sector seek/read/write, SSD image mounting, and DFS catalog parsing (`*CAT`, `*RUN`, `*EXEC !BOOT`).
- **Video Subsystem (CRTC 6845 + Video ULA):**
  - MODE 1: 320×256 4-color bitmap graphics.
  - MODE 2: 160×256 16-color bitmap graphics.
  - MODE 5: 160×256 4-color bitmap graphics.
  - MODE 7: 40×25 character Teletext (Mullard SAA5050) with mosaic graphics and control codes.
  - Authentic 8-scanline character block memory organization.
- **Sound Synthesis (TI SN76489AN):**
  - 3 programmable square-wave tone channels with 10-bit dividers.
  - 1 noise channel with 15-bit LFSR (white and periodic modes).
  - 16 logarithmic attenuation levels calibrated to eliminate clipping.
  - Windows WaveOut audio output (`winmm.dll`) streaming 44.1 kHz 16-bit mono PCM in 882-sample blocks aligned to 50 Hz frames.
- **System 6522 VIA & Keyboard Matrix:**
  - 10×8 physical keyboard matrix mapping.
  - IC32 addressable latch emulation.
  - Automatic keyboard matrix scanning with CA2 interrupt assertion for MOS circular buffer input (`GET` / `OSRDCH`).
- **Ferranti Tube ULA & Second Processor:**
  - 4 bidirectional communication FIFO channels with backpressure flags.
  - 65C02 parasite CPU running at 3.0 MHz with isolated 64 KB RAM.
  - Parasite bootstrap sequence verified with Chuckie Egg 2023 Anniversary Edition.
- **Native Windows GUI Emulator:**
  - Real-time 50 Hz frame timing synchronized to high-resolution system timers.
  - Full keyboard handling with hotkeys (`ESC` Exit, `Pause` Pause/Resume, `F10` Reset).
- **Test Suite:** Comprehensive .NET 8 xUnit test harness covering 36 automated test cases (100% passing).
- **Documentation Suite:** Full technical documentation (`ARCHITECTURE.md`, `HARDWARE_REFERENCE.md`, `GAME_COMPATIBILITY.md`, `THIRD_PARTY_MEDIA.md`, `BUILDING.md`, `TESTING.md`, `SCREENSHOTS.md`).
- **Whitepaper PDF:** Publication-grade engineering whitepaper generated via ReportLab (`Acorn_BBC_Micro_Model_B_Reconstruction_Report.pdf`).
