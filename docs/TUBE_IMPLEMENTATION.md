# Acorn Tube Second Processor Implementation

## Overview

This document describes the emulation architecture for the **Acorn 6502/65C02 Second Processor** ("Cheese Wedge" and Master Turbo coprocessors) implemented in Project 003.

The Tube architecture splits execution between:
1. **Host I/O Processor (IOP):** Acorn BBC Micro Model B (MOS 6502 @ 2.0 MHz, 32 KB RAM, MOS 1.20).
2. **Parasite Coprocessor:** 65C02 CPU @ 3.0 MHz with 64 KB dedicated dynamic RAM.
3. **Tube ULA:** Custom Ferranti gate array providing four bidirectional FIFO/latch channels and control registers.

---

## Memory Map

### Host Space (`$FEE0` - `$FEE7`)

| Address | Register | Description |
|---|---|---|
| `$FEE0` | R1STAT / CTRL | Status of Channel 1 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `control[5:0]`). Writes modify 7-bit Tube Control Register. |
| `$FEE1` | R1DATA | Channel 1 Data Register (Host to Parasite 24-byte FIFO; Parasite to Host 1-byte latch). |
| `$FEE2` | R2STAT | Status of Channel 2 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEE3` | R2DATA | Channel 2 Data Register (Bidirectional 1-byte latches). |
| `$FEE4` | R3STAT | Status of Channel 3 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEE5` | R3DATA | Channel 3 Data Register (Host to Parasite 24-byte FIFO; Parasite to Host 24-byte FIFO). |
| `$FEE6` | R4STAT | Status of Channel 4 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEE7` | R4DATA | Channel 4 Data Register (Bidirectional 1-byte latches). |

### Parasite Space (`$FEF8` - `$FEFF`)

| Address | Register | Description |
|---|---|---|
| `$FEF8` | R1STAT | Status of Channel 1 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `control[5:0]`). |
| `$FEF9` | R1DATA | Channel 1 Data Register. |
| `$FEFA` | R2STAT | Status of Channel 2 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEFB` | R2DATA | Channel 2 Data Register. |
| `$FEFC` | R3STAT | Status of Channel 3 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEFD` | R3DATA | Channel 3 Data Register. |
| `$FEFE` | R4STAT | Status of Channel 4 (Bit 7: Read Ready, Bit 6: Write Ready, Bits 5..0: `0x3F`). |
| `$FEFF` | R4DATA | Channel 4 Data Register. |

---

## Tube ULA Control Protocol

Writing to `$FEE0` on the Host side modifies the internal 7-bit Control Register using a bit-set / bit-clear protocol:

- **Bit 7 = 1 (Bit-Set):** `ControlRegister |= (value & 0x7F)`
- **Bit 7 = 0 (Bit-Clear):** `ControlRegister &= ~(value & 0x7F)`

### Control Register Bits:
- **Bit 6 (`t_flag`):** Tube Reset (holds all internal FIFOs in reset).
- **Bit 5 (`p_flag`):** Parasite Reset (holds parasite in reset while set; releasing executes reset vector).
- **Bit 4 (`v_flag`):** Two-byte transfer mode on Channel 3.
- **Bit 3 (`m_flag`):** Parasite NMI enable from Channel 3 data available.
- **Bit 2 (`j_flag`):** Parasite IRQ enable from Channel 4 data available.
- **Bit 1 (`i_flag`):** Parasite IRQ enable from Channel 1 data available.
- **Bit 0 (`q_flag`):** Host IRQ enable from Channel 4 data available.

---

## Dual-CPU Scheduling

The emulator implements deterministic cycle interleaving:
- Host runs at 2.0 MHz.
- Parasite runs at 3.0 MHz (ratio 1.5 parasite cycles per host cycle).
- On each host cycle, the parasite cycle accumulator advances by 3 cycles for every 2 host cycles.
- Edge-triggered NMI detection on both cores ensures that continuous interrupt conditions do not trap the CPU in endless vector dispatch loops.

---

## Command Line Interface

```powershell
# Run with Tube enabled (using internal bootstrap client)
.\src\BbcMicro.Emulator\bin\Debug\net8.0\BbcMicro.Emulator.exe --tube

# Run with an authentic external Acorn Tube Client ROM
.\src\BbcMicro.Emulator\bin\Debug\net8.0\BbcMicro.Emulator.exe --tube --tube-rom roms\tube.rom

# Run with an authentic external Acorn DNFS Host ROM
.\src\BbcMicro.Emulator\bin\Debug\net8.0\BbcMicro.Emulator.exe --tube --dnfs-rom roms\dnfs.rom

# Enable bounded Tube ULA debug trace
.\src\BbcMicro.Emulator\bin\Debug\net8.0\BbcMicro.Emulator.exe --tube --trace-tube
```

---

## Component Status Matrix

| Component | Status | Description |
|---|---|---|
| **Tube ULA Core** | **COMPLETE** | Full 4-channel bidirectional FIFOs and latches with authentic bit-set/bit-clear control. |
| **65C02 Core** | **COMPLETE** | CMOS instructions (STZ, BRA, PHX/PLX, PHY/PLY, zero-page indirect). |
| **MOS 1.20 Tube Detection** | **COMPLETE** | Hardware probe at `$DB38` and OSBYTE &EA ($027A) supported. |
| **Dual-CPU Scheduler** | **COMPLETE** | 2 MHz Host / 3 MHz Parasite interleaved execution. |
| **Chuckie Egg 2023 Loader** | **TRANSFERRED** | 566-byte R3 bootstrap, 702-byte R1 stream, OSFIND `Game` streaming verified. |
| **1983 Model B Chuckie Egg** | **PLAYABLE** | Authentic Model B single-CPU release loads, switches Mode 1, and renders. |
