# REA-02: BBC MICRO MODEL B FIRMWARE IDENTIFICATION & PROVENANCE

**Project:** 003 - Acorn BBC Micro Model B Reconstruction & Emulation  
**Series:** Historical Computer Reverse Engineering  
**Researcher:** ScientificBrad  
**Date:** 2026-11-08  
**Repository:** `ScientificBrad/bbc-micro-core`

---

## 1. Overview & Architectural Role

The BBC Micro Model B relies on a modular, bank-switched memory model where the operating system, high-level languages, and filing systems reside in separate ROM chips. Unlike Commodore architectures (which feature tightly integrated Kernal and BASIC in unified address decoding), the Acorn MOS establishes a strict architectural boundary via indirect vector tables, sideways ROM banking, and OS-standardized call interfaces (`OSBYTE`, `OSWORD`, `OSCLI`, and Stream I/O).

---

## 2. Acorn Machine Operating System (MOS) 1.20

### 2.1 Historical Identification
- **Release Date:** Autumn 1982
- **Predecessors:** MOS 0.10 (Provisional early OS), MOS 1.00 (First production Model A/B)
- **Primary Authors:**
  - Roger Wilson (later Sophie Wilson): Architecture, VDU, Assembler, System software
  - Steve Furber: Hardware integration, Ferranti ULA coordination
  - Peter Miller: Sound and speech subsystem
  - Graham Tebby: High-resolution graphics and Plotting engine
  - Chris Turner, Paul Bond: Hardware architecture and manufacturing

### 2.2 Operational Mechanics
MOS 1.20 is the definitive, gold-standard operating system for the BBC Micro Model B. It manages:
1. **Reset Sequence (`$D9CD`):**
   - Disables interrupts (`SEI`).
   - Sets stack pointer to `$01FF`.
   - Programs the 6845 CRTC and Video ULA into standard Mode 7 (Teletext).
   - Initializes zero-page and page 2/3 OS workspace variables from default tables (`$D940`).
   - Copies default OS vector table (`$0200-$0235`) from ROM.
   - Initialises the System VIA 6522 and User VIA 6522.
   - Clears memory and sets memory pointers (`PAGE`, `OSHWM`, `HIMEM`).
   - Polls sideways ROM sockets (banks 0 to 15) to detect language and service ROMs.
   - Selects highest numbered valid Language ROM (typically Bank 15: BBC BASIC).
   - Jumps to the language entry point with `A = 0x01` (Language Reset).

2. **The Famous MOS Easter Egg / Credits:**
   - In physical ROM manufacture, memory locations `$FC00-$FEFF` (which physically correspond to the FRED, JIM, and SHEILA memory mapped I/O space) cannot be read by software because hardware address decoders route read requests to peripheral chips.
   - In the ROM mask, Acorn programmed a 768-byte tribute string crediting the designers:
     `(C) 1981 Acorn Computers Ltd.Thanks are due to the following contributors...`
   - Reading this address on actual hardware returns peripheral state or floating bus lines (`$FF`).
   - Toby Nelson's reassembly reproduces this exact original mask byte stream (`MD5: 0a59a5ba15fe8557b5f7fee32bbd393a`), while runtime dumps reflect the open bus state (`MD5: 3f724370ece4e4b790f26a8eca196e89`).

---

## 3. BBC BASIC II

### 3.1 Historical Identification
- **Release Date:** 1982
- **Author:** Sophie Wilson (Acorn Computers)
- **Binary Size:** 16,384 bytes
- **ROM Sockets:** Installed in sideways slot 15.
- **Identifier:** Title string `"BASIC"`, version `1`, copyright `(C)1982 Acorn`.

### 3.2 Evolution from BASIC I to BASIC II
- Fixed bugs in string handling and memory allocations present in original Model A/B BASIC I.
- Added the `OPENUP` file system keyword for opening existing files for update.
- Added error trapping enhancement `ON ERROR LOCAL ... RESTORE ERROR`.
- Added `CALL` and built-in inline 6502 assembler enhancements.
- Retained ultra-fast tokenized execution, outperforming Microsoft BASIC implementations of the era by 3x to 5x.

### 3.3 Sideways ROM Header & Language Entry
- `$8000`: `C9 01` (`CMP #$01`)
- `$8002`: `F0 1F` (`BEQ +$1F` -> jump to cold start initialization)
- `$8004`: `60` (`RTS` - ignore service calls)
- `$8005`: `EA` (`NOP`)
- `$8006`: `0x60` (Bit 6 set = Language ROM, Bit 7 clear = Not a service handler)
- `$8007`: `0x0E` (Pointer offset to copyright header)

---

## 4. Acorn Disc Filing System (DFS) 0.90

### 4.1 Historical Identification
- **Release Date:** 1982
- **Binary Size:** 8,192 bytes
- **Controller Target:** Intel 8271 Programmable Floppy Disk Controller
- **Socket:** Bank 14 (or Bank 4 on early motherboards)

### 4.2 Disk Architecture & Format
Acorn DFS 0.90 implements the classic single-density (FM) disk layout:
- **Tracks:** 40 tracks standard (expandable to 80 tracks)
- **Sectors per Track:** 10 sectors (numbered 0 to 9)
- **Sector Size:** 256 bytes
- **Sides:** Single-sided (Side 0, Side 1 formatted independently)
- **Capacity:** 100 KB per side (40-track) or 200 KB per side (80-track)
- **Directory Structure:**
  - Stored in Track 0, Sectors 0 and 1.
  - Sector 0 holds disk title (first 8 bytes) and 31 directory entries: filenames (7 characters) and directory letter (1 character).
  - Sector 1 holds disk title remainder, sequence number, file count, and file metadata:
    - Load address (18-bit)
    - Execution address (18-bit)
    - File length (18-bit)
    - Start sector (10-bit)
    - Locked status / read-only flag

### 4.3 Service ROM Mechanics
- Intercepts MOS service calls via `$8003` (`JMP $9455`).
- Responds to Service 3 (Auto-boot / Shift-Break), Service 4 (Unknown command / Star command trap), Service 8 (Filing system identification), and Service 9 (`*HELP`).
- Re-routes OS file system vectors:
  - `FILEV` (`$0212`)
  - `ARGSV` (`$0214`)
  - `BGETV` (`$0216`)
  - `BPUTV` (`$0218`)
  - `FINDV` (`$021C`)
  - `FSCV`  (`$021E`)
