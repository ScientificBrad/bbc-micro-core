# REA-03: BBC MICRO MODEL B MEMORY ARCHITECTURE & I/O SUBSYSTEM

**Project:** 003 - Acorn BBC Micro Model B Reconstruction & Emulation  
**Series:** Historical Computer Reverse Engineering  
**Researcher:** ScientificBrad  
**Date:** 2026-11-08  
**Repository:** `ScientificBrad/bbc-micro-core`

---

## 1. System Memory Map Overview

The BBC Micro Model B utilizes a 6502 16-bit address space (64 KB) subdivided into system RAM, bank-switched sideways ROMs, memory-mapped I/O, and the Acorn MOS.

```text
+------------------------------------------+ $FFFF
| Acorn MOS 1.20 (Operating System)        |
| - Standard OS Jump Table ($FFB6-$FFF7)   |
| - Hardware Vectors ($FFFA-$FFFF)         |
+------------------------------------------+ $FF00
| SHEILA ($FE00-$FEFF) - System I/O        |
| JIM    ($FD00-$FDFF) - External 1MHz Bus |
| FRED   ($FC00-$FCFF) - External 1MHz Bus |
+------------------------------------------+ $FC00
| Acorn MOS 1.20 (Operating System Code)   |
+------------------------------------------+ $C000
| Sideways ROM Window (16 KB Banked)       |
| Banks 0-15 (BASIC II, DFS 0.90, etc.)    |
+------------------------------------------+ $8000
| System RAM (32 KB Total)                 |
| Screen Display Memory (Modes 0-7)        |
| - Mode 7 (Teletext): $7C00-$7FFF (1 KB)  |
| - Mode 0/1/2 (Hi-Res): $3000-$7FFF (20KB)|
|                                          |
| User Program Area (PAGE to HIMEM)        |
| OSHWM / PAGE: $0E00 (Cassette) / $1900   |
+------------------------------------------+ $0800
| Sound, DFS Workspace, Envelopes          |
+------------------------------------------+ $0400
| OS Buffers, VDU Variables, Sound Buffers |
+------------------------------------------+ $0300
| MOS Indirect Vector Table ($0200-$0235)  |
+------------------------------------------+ $0200
| 6502 Hardware Stack ($0100-$01FF)        |
+------------------------------------------+ $0100
| Zero Page ($0000-$00FF)                  |
| - $00-$8F: MOS Zero Page Workspace       |
| - $90-$FF: Language / User Workspace     |
+------------------------------------------+ $0000
```

---

## 2. Low Memory Layout ($0000-$07FF)

### 2.1 Zero Page Allocation
- `$0000-$008F`: Operating System workspace (VDU variables, timers, interrupt flags, tape storage counters).
- `$0090-$009F`: Current filing system scratch space.
- `$00A0-$00A7`: Econet workspace.
- `$00A8-$00AF`: Secondary processor / Tube communication buffers.
- `$00B0-$00BF`: Sound and speech synthesiser workspace.
- `$00C0-$00DF`: User / Machine language program scratch area.
- `$00E0-$00FF`: Language workspace (BBC BASIC variable pointers, line number pointers, token tables).

### 2.2 Vector Table ($0200-$0235)
All MOS calls route through RAM vector tables, allowing software, filing systems, and language ROMs to hook, extend, or intercept operations:

| Address | Symbol | Description | Default MOS Target |
| :--- | :--- | :--- | :--- |
| `$0200` | `USERV` | User unhandled break / escape | `$DC11` |
| `$0202` | `BRKV` | Software BRK instruction handler | `$DC1C` |
| `$0204` | `IRQ1V` | Main hardware interrupt entry | `$DC1C` |
| `$0206` | `IRQ2V` | Secondary interrupt handler | `$DC7E` |
| `$0208` | `CLIV` | Command Line Interpreter (`*` commands) | `$DEC5` |
| `$020A` | `BYTEV` | `OSBYTE` call dispatcher | `$E5B3` |
| `$020C` | `WORDV` | `OSWORD` call dispatcher | `$E5B9` |
| `$020E` | `WRCHV` | Character output (`OSWRCH`) | `$E0A4` |
| `$0210` | `RDCHV` | Character input (`OSRDCH`) | `$DEC5` |
| `$0212` | `FILEV` | High-level file operations (`OSFILE`) | `$F135` |
| `$0214` | `ARGSV` | Read/write file arguments (`OSARGS`) | `$F135` |
| `$0216` | `BGETV` | Read byte from open file (`OSBGET`) | `$F135` |
| `$0218` | `BPUTV` | Write byte to open file (`OSBPUT`) | `$F135` |
| `$021A` | `GBPBV` | Read/write block of bytes (`OSGBPB`) | `$F135` |
| `$021C` | `FINDV` | Open or close file (`OSFIND`) | `$F135` |
| `$021E` | `FSCV` | Filing system control (`OSFSC`) | `$F135` |
| `$0220` | `EVNTV` | Operating system events | `$E20E` |
| `$0222` | `UPTV` | User printer output | `$E0A4` |
| `$0224` | `NETV` | Network / Econet driver | `$E0A4` |
| `$0226` | `VDUV` | Direct VDU vector | `$C4C0` |
| `$0228` | `KEYV` | Keyboard scan routine | `$EEDA` |
| `$022A` | `INSV` | Insert character into buffer | `$E20E` |
| `$022C` | `REMV` | Remove character from buffer | `$E20E` |
| `$022E` | `CNPV` | Count / purge character buffer | `$E0A4` |

---

## 3. Video Display Architecture & RAM Modes

The Video subsystem uses a Motorola 6845 CRTC paired with the Ferranti Video ULA and 32 KB shared RAM. The screen memory wraps from `$7FFF` down towards `HIMEM`:

| Video Mode | Screen Geometry | Colours | Memory Used | Start Address | Default `HIMEM` | Description |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Mode 0** | 640 x 256 (80 x 32 text) | 2 | 20 KB | `$3000` | `$3000` | High-resolution monochrome graphics |
| **Mode 1** | 320 x 256 (40 x 32 text) | 4 | 20 KB | `$3000` | `$3000` | Standard 4-colour graphics |
| **Mode 2** | 160 x 256 (20 x 32 text) | 16 (8+8 flash) | 20 KB | `$3000` | `$3000` | Multi-colour graphics / low resolution |
| **Mode 3** | 80 x 25 text only | 2 | 16 KB | `$4000` | `$4000` | High-density text display |
| **Mode 4** | 320 x 256 (40 x 32 text) | 2 | 10 KB | `$5800` | `$5800` | Medium-resolution graphics |
| **Mode 5** | 160 x 256 (20 x 32 text) | 4 | 10 KB | `$5800` | `$5800` | Medium-resolution 4-colour graphics |
| **Mode 6** | 40 x 25 text only | 2 | 8 KB | `$6000` | `$6000` | Low-overhead text display |
| **Mode 7** | 40 x 25 Teletext | 8 | 1 KB | `$7C00` | `$7C00` | SAA5050 hardware character generator |

At startup, the BBC Micro defaults to **Mode 7**, leaving 28 KB of free RAM for user programs (`PAGE = $0E00`, `HIMEM = $7C00`).

---

## 4. Sideways ROM Banking Mechanics

The region `$8000-$BFFF` (16 KB) is dynamically mapped to one of 16 sideways slots:
- Selected by writing the slot number (0-15) to memory address `$FE30` (ROM Select Latch) or through System VIA Port B.
- Slot allocation conventions:
  - **Slot 15:** Language ROM (BBC BASIC II)
  - **Slot 14:** Filing System ROM (DFS 0.90)
  - **Slots 0-13:** Utilities, View Word Processor, Forth, Lisp, or Sideways RAM.

During cold reset, MOS 1.20 executes a discovery loop from Slot 15 down to 0, reading `$8006` (type byte) and verifying the copyright string `\0)C(` at `$8000 + ($8007)`. The highest valid Language ROM is automatically selected and entered.

---

## 5. Memory-Mapped I/O (SHEILA: $FE00-$FEFF)

Internal peripherals are memory-mapped into page `$FE`:

```text
$FE00-$FE01: 6845 CRTC (Address Register, Data Register)
$FE08-$FE09: 6850 ACIA (Status/Control, Tx/Rx Data)
$FE10-$FE1F: Serial ULA
$FE20-$FE21: Video ULA (Control Register, Palette Register)
$FE30-$FE3F: ROM Select Register (Latch)
$FE40-$FE4F: System VIA 6522:
  - Port A: Keyboard row scan & Sound data latch
  - Port B: Keyboard column read & Speech / Display address latch
  - Timers 1 & 2: 100Hz system timer and sound envelope timing
$FE60-$FE6F: User VIA 6522:
  - Port A: Centronics 8-bit parallel printer
  - Port B: User extension port
$FE80-$FE84: Intel 8271 Floppy Disk Controller (DFS 0.90)
$FEA0-$FEA3: MC6854 Advanced Data Link Controller (Econet)
$FEC0-$FEC3: 7002 Analogue-to-Digital Converter
$FEE0-$FEE7: Tube ULA (Second processor interface)
```
