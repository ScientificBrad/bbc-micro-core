# Acorn BBC Micro Model B Architecture

---

## 1. System Overview

The **Acorn BBC Microcomputer Model B (1981)** is an iconic British 8-bit personal computer engineered by Acorn Computers Ltd (founded by Hermann Hauser and Chris Curry, with technical leadership by Sophie Wilson and Steve Furber) for the British Broadcasting Corporation's Computer Literacy Project.

The system is characterized by high integration, rigorous bus timing, hardware memory-mapped peripherals, and clean expansion capabilities via sideways ROM sockets and the high-speed Tube interface.

```
       +---------------------------------------------------------------------------------+
       |                  ACORN BBC MICRO MODEL B SYSTEM ARCHITECTURE                    |
       +---------------------------------------------------------------------------------+
                                                |
                               +----------------+----------------+
                               |     MOS Technology 6502 CPU     |
                               |          (2.00 MHz)             |
                               +----------------+----------------+
                                                |
               +--------------------------------+--------------------------------+
               |                                |                                |
        +--------------+                 +--------------+                 +--------------+
        |   32 KB RAM  |                 | 16 KB RomBank|                 | 16 KB MOS    |
        | $0000-$7FFF  |                 | $8000-$BFFF  |                 | $C000-$FFFF  |
        | Base Memory  |                 | BASIC/DFS/Etc|                 | Acorn MOS1.20|
        +--------------+                 +--------------+                 +--------------+
               |                                                                 |
        +--------------+                 +-----------------------------------------------+
        |  Video ULA   |                 |             Memory-Mapped I/O ($FE00-$FEFF)   |
        | + CRTC 6845  |<===============>| $FE00-$FE07: Motorola 6845 CRTC              |
        | (Modes 0-7)  |                 | $FE20-$FE21: Ferranti Video ULA               |
        +--------------+                 | $FE30-$FE33: ROM Select Latch                 |
               |                         | $FE40-$FE4F: System VIA (6522) -> SN76489, KBD|
        +--------------+                 | $FE60-$FE6F: User VIA (6522)   -> User Port   |
        | TI SN76489AN |<----------------| $FE80-$FE87: Intel 8271 Floppy Disk Controller|
        | 4-Ch Audio   |                 | $FEE0-$FEE7: Ferranti Tube ULA Host Registers |
        +--------------+                 +-----------------------------------------------+
```

---

## 2. Processor Architecture: MOS Technology 6502

- **Clock Frequency:** Exactly 2.00 MHz (500 ns per cycle, synchronized to system 16 MHz master crystal divided by 8).
- **Address Bus:** 16-bit addressing (64 KB physical space).
- **Data Bus:** 8-bit bidirectional data bus.
- **Instruction Cycle Accounting:** Instructions take between 2 and 7 cycles, with page-boundary penalty cycles accurately modeled.
- **Interrupt System:**
  - **IRQ (Interrupt Request):** Level-sensitive, maskable via Status Register `I` flag. Driven by System 6522 VIA (timers, vertical sync, keyboard) and User 6522 VIA. Vector at `$FFFE-$FFFF`.
  - **NMI (Non-Maskable Interrupt):** Edge-triggered, non-maskable. Driven by the Intel 8271 Floppy Disk Controller during high-speed byte data transfers and the Ferranti Tube ULA. Vector at `$FFFA-$FFFB`.
  - **RESET:** Initializes CPU state, loads program counter from `$FFFC-$FFFD` (points to MOS 1.20 cold start routine at `$D9CD`).

---

## 3. Memory Map & Sideways ROM Architecture

The 64 KB memory space is organized into five functional regions:

```
+---------------+ $FFFF
|  Acorn MOS    | 16 KB Fixed Operating System ROM (MOS 1.20)
|  Firmware     | Vectors at $FFFA-$FFFF, Kernel entry points at $FF00-$FFF7
+---------------+ $C000
|  Memory-      | 256 bytes hardware registers ($FE00-$FEFF)
|  Mapped I/O   | Paged ROM select, CRTC, Video ULA, VIAs, 8271 FDC, Tube
+---------------+ $FE00
|  Sideways     | 16 KB Paged Sideways ROM/RAM Bank ($8000-$BFFF)
|  ROM Space    | Switched via write to $FE30 (Banks 0-15)
+---------------+ $8000
|  Display RAM  | Dynamic video memory buffer (size depends on active MODE)
|  & High Mem   | MODE 1/2: $3000-$7FFF (20 KB); MODE 7: $7C00-$7FFF (1 KB)
+---------------+ 
|  User RAM     | BBC BASIC program workspace, OS system variables
|  $0E00-$7FFF  | PAGE defaults to $0E00 (or $1900 with DFS active)
+---------------+ $0E00
|  DFS Buffer   | Work area allocated when Acorn DFS is loaded ($0E00-$18FF)
+---------------+ $0900
|  OS Workspace | Sound buffers, envelope tables, font definition cache
+---------------+ $0400
|  VDU Drivers  | Text cursor, graphics coordinates, window boundaries ($0300)
+---------------+ $0300
|  MOS Variables| Interrupt vectors ($0200-$02FF), keyboard buffer ($02E0)
+---------------+ $0200
|  Hardware Stk | 6502 Stack ($0100-$01FF)
+---------------+ $0100
|  Zero Page    | Direct Addressing registers, OS pointers, DFS state ($00-$FF)
+---------------+ $0000
```

### Sideways ROM Switching ($FE30)
Writing to memory address `$FE30` sets the active 16 KB ROM bank mapped into `$8000-$BFFF`.
- **Bank 15:** BBC BASIC II (Language ROM, service type 0x80).
- **Bank 14:** Acorn DFS 0.90 (Disc Filing System ROM, service type 0x82).
- **Banks 0–13:** Available for expansion ROMs (View, Pascal, DNFS, Telesoft, Sideways RAM).

---

## 4. Intel 8271 Floppy Disk Controller & Acorn DFS

The Intel 8271 is a programmable floppy disk controller supporting single-density (FM) recording:
- **Registers:** Memory-mapped at `$FE80-$FE84` (Status/Command `$FE80`, Result/Parameter `$FE81`, Reset `$FE82`, Data `$FE84`).
- **NMI Byte Streaming:** Sector data transfer is driven via the 6502 NMI interrupt. Each incoming or outgoing byte triggers an NMI, which executes an ultra-tight transfer loop in DFS ROM (`$9000`).
- **Drive Geometry:** Single-sided (SSD) disk images contain 80 tracks, 10 sectors per track, 256 bytes per sector (total 204,800 bytes).
- **Catalogue Format:** Sector 0 contains the first 8 characters of filenames and disk title; Sector 1 contains load addresses, execution addresses, file lengths, and start sectors.

---

## 5. Video Subsystem: Ferranti Video ULA & Motorola 6845 CRTC

The BBC Micro video subsystem utilizes two cooperating chips:
1. **Motorola 6845 CRTC:** Generates horizontal and vertical timing, character row addresses, scanline counters, and screen memory address generation.
2. **Ferranti Video ULA (IC12):** Converts character bytes into serial RGB pixel signals at 16 MHz or 8 MHz dot clocks, and maps 4-bit logical colors to 3-bit physical RGB values via its internal palette registers.

### 8-Scanline Character Interleaving
Unlike conventional linear framebuffers, screen memory is arranged into 8-scanline character blocks. Each byte within a character cell represents a horizontal pixel slice across 8 successive raster lines:
$$\text{Memory Address} = \text{ScreenBase} + (\text{Row} \times \text{Cols} \times 8) + (\text{Col} \times 8) + \text{Scanline}$$

### Display Modes
- **MODE 1:** $320 \times 256$, 4 colors (2 bits per pixel), 20 KB display RAM (`$3000-$7FFF`).
- **MODE 2:** $160 \times 256$, 16 colors (4 bits per pixel), 20 KB display RAM (`$3000-$7FFF`).
- **MODE 5:** $160 \times 256$, 4 colors (2 bits per pixel), 10 KB display RAM (`$5800-$7FFF`).
- **MODE 7:** $40 \times 25$ characters, Teletext (Mullard SAA5050 character generator), 1 KB display RAM (`$7C00-$7FFF`).

---

## 6. Sound Synthesis: Texas Instruments SN76489AN

The sound subsystem is powered by a Texas Instruments SN76489AN programmable sound generator:
- **Input Clock:** 4.00 MHz crystal frequency.
- **Channels:** 3 independent square-wave tone channels + 1 noise generator.
- **Tone Frequency:** 10-bit divider ($f = 4{,}000{,}000 / (32 \times N)$).
- **Noise Generator:** 15-bit Linear Feedback Shift Register (LFSR) with Periodic and White noise modes.
- **Attenuation:** 16 logarithmic volume steps (0 dB to -28 dB in 2 dB steps, plus total silence).
- **Interface:** Controlled via System 6522 VIA Port A (data lines) and IC32 addressable latch output 0 (active-low `/WE` strobe).
- **Host Output:** 44,100 Hz 16-bit mono audio stream buffered directly to the Windows WaveOut subsystem (`winmm.dll`).

---

## 7. Input: Keyboard Matrix & System 6522 VIA

- **Keyboard Matrix:** 10 columns by 8 rows matrix, polled via System 6522 VIA Port A and IC32 addressable latch.
- **Auto-Scan & CA2 Interrupt:** When auto-scan is active, pressing any physical key asserts the VIA CA2 interrupt line (`IFR bit 0`), waking the MOS 100 Hz interrupt service routine and populating the circular input buffer at `$02E0`.
