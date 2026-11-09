# REA REPORT 05: CHUCKIE EGG COMPATIBILITY & INTEL 8271 RECONSTRUCTION

**Researcher:** ScientificBrad  
**Series:** Historical Computer Reverse Engineering  
**Project:** 003: Acorn BBC Micro Model B (1981)  
**Target Software:** *Chuckie Egg 2023 (40th Anniversary Edition)*, Sam Skivington / A&F Software  
**Target Media:** `games/Disc999-chuckieEgg2023Web.ssd`  
**Host Subsystems:** Acorn MOS 1.20 (`$C000-$FFFF`), Acorn DFS 0.90 (`$8000-$9FFF`), Intel 8271 FDC (`$FE80-$FE84`)  

---

## 1. EXECUTIVE STATUS & MILESTONE MATRIX

| Milestone | Status | Verified Evidence |
|---|---|---|
| **DISK MOUNT** | **PASS** | 80-track single-sided SSD mounted and decoded (204,800 bytes) |
| **FDC 8271 EMULATION** | **PASS** | Full registers, command state machine, and NMI streaming verified |
| **DFS 0.90 CATALOGUE (`*CAT`)** | **PASS** | Genuine DFS reads Sectors 0 & 1 via 8271 NMI, displays files |
| **BOOTS (AUTOBOOT)** | **PASS** | Boot Option 3 executes `*EXEC !BOOT` -> `*/Boot` -> loads `Boot` |
| **LOADS** | **PASS** | Genuine DFS streams 12 sectors (3,066 bytes) of `$.Boot` to `&6000` |
| **TITLE SCREEN** | **PASS** | `$.Boot` executes on 6502, switches Mode 7, displays authentic banner |
| **INTERACTIVE / PLAYABLE** | **PENDING** | Requires Acorn 6502 Second Processor (Tube) coprocessor emulation |

### Status Summary:
- **BOOTS:** **YES**
- **LOADS:** **YES**
- **TITLE:** **YES**
- **INTERACTIVE:** **PENDING TUBE 2ND PROCESSOR**
- **PLAYABLE:** **PENDING TUBE 2ND PROCESSOR**

---

## 2. DISC PROVENANCE & GEOMETRY

The game disc provided in the repository was inspected and verified:

```text
File: games/Disc999-chuckieEgg2023Web.ssd
Size: 204,800 bytes
Geometry: 80 Tracks, Single-Sided, 10 Sectors/Track, 256 Bytes/Sector
Tracks: 80 (0 to 79)
Sectors: 10 (0 to 9) per track
Total Sectors: 800 sectors = 204,800 bytes
SHA-256: 639abc21270e51c9a8c8dc84cb58ac955e5bc5e787395796c5e1f906dd39616f
MD5:     46bfd05ec58d1163e72bc7b71329c4ba
```

### DFS Catalogue Structure

Reading Sector 0 and Sector 1 from `Disc999-chuckieEgg2023Web.ssd` decodes the authentic catalogue:

- **Disc Title:** `ChuckieEgg23` (Part 1 in Sector 0 bytes 0-7, Part 2 in Sector 1 bytes 0-3)
- **Cycle Count:** 0
- **File Count:** 5 files
- **Boot Option:** `0x30` (Bits 4-5 = `11` = **Option 3: `*EXEC !BOOT`**)

#### Catalogue Entries:
| Filename | Directory | Load Address | Execution Address | Length (bytes) | Start Sector |
|---|---|---|---|---|---|
| `!Boot` | `$` | `&000000` | `&000000` | 7 | 2 |
| `Boot` | `$` | `&036000` | `&036000` | 3,066 | 3 |
| `Game` | `$` | `&000000` | `&000000` | 41,834 | 15 |
| `UserDat`| `$` | `&000000` | `&000000` | 144 | 179 |
| `EditDat`| `$` | `&000000` | `&000000` | 1,364 | 180 |

Note on `Boot` address: Load and Exec are encoded as `&036000` (`&6000` with high bits set to `3` = execute on host I/O processor).

---

## 3. INTEL 8271 CONTROLLER ARCHITECTURE & IMPLEMENTATION

The Intel 8271 Programmable Floppy Disk Controller is memory-mapped on the BBC Micro at `$FE80-$FE84` (mirrored across `$FE80-$FE9F`):

| Address | Read | Write | Function |
|---|---|---|---|
| `$FE80` | Status Register | Command Register | Issues high-level commands / returns chip status flags |
| `$FE81` | Result Register | Parameter Register | Command parameters / result and error codes |
| `$FE82` | — | Reset Register | Hardware reset |
| `$FE84` | Data Register | Data Register | Non-DMA data streaming register (reads/writes data bytes) |

### Status Register (`$FE80` Read) Flags:
- **Bit 7 (`0x80`):** Command Busy (1 = busy executing, 0 = idle)
- **Bit 6 (`0x40`):** Command Register Full
- **Bit 5 (`0x20`):** Parameter Register Full (0 = ready for parameter, 1 = full)
- **Bit 4 (`0x10`):** Result Register Full (1 = result byte ready in `$FE81`)
- **Bit 3 (`0x08`):** Interrupt Request (INT) (1 = command completion interrupt active)
- **Bit 2 (`0x04`):** Non-DMA Data Request (DRQ) (1 = data byte ready in `$FE84`)

### NMI Streaming Protocol:
DFS 0.90 handles all floppy data transfers non-DMA via the 6502 Non-Maskable Interrupt (NMI):
1. MOS 1.20 vectors NMI (`$FFFA-$FFFB`) to `$0D00`.
2. DFS 0.90 claims the NMI vector on reset and copies its NMI service routine into `$0D00-$0D9E`.
3. When DFS issues a **Read Sectors** (`0x13`) command:
   - For each byte of the sector, the 8271 places the byte in `$FE84`, sets Status Bit 2 (`DRQ`), and asserts NMI.
   - The 6502 enters the NMI handler at `$0D00`, reads `$FE80` (verifies Bit 2 = 1), and reads `$FE84`.
   - Reading `$FE84` clears `DRQ` and clears NMI.
   - The 8271 schedules the next byte with authentic 64-microsecond FM pacing (64 to 128 CPU cycles), giving the 6502 time to execute `STA ($A6),Y; INC $A6; RTI`.
   - Once all `Count * 256` bytes are transferred, the 8271 clears `DRQ`, clears `Busy`, sets Bit 3 (`INT`) and Bit 4 (`Result Full`), enqueues success code `0x00`, and asserts NMI for completion.
   - DFS mainline code (polling `BIT $FE80; BMI`) detects completion, reads `$FE81`, and exits cleanly.

---

## 4. REVERSE ENGINEERING CHUCKIE EGG 2023 (40th ANNIVERSARY)

### The Tube Co-Processor Requirement
Disassembly of `$.Boot` (`games/boot_dump.bin`) revealed critical architectural findings:
1. `$.Boot` sets up Mode 7 and displays the authentic 40th anniversary banner:
   ```text
   samskivington.com PRESENTS
   Chuckie Egg 2023
   40th Anniversary Edition
   ```
2. At `$699F`, `$.Boot` calls **OSBYTE 234 (`$EA`)**:
   ```assembly
   $699F: LDA #$EA        ; OSBYTE 234: Read Tube Presence
   $69A1: LDX #$00
   $69A3: LDY #$00
   $69A5: JSR $FFF4       ; OSBYTE entry
   $69A8: CPX #$00        ; X != 0 indicates Tube present
   $69AA: BNE $69AE       ; If present -> CLC, RTS (Success)
   $69AC: SEC             ; If not present -> SEC, RTS (Error: "No 2nd Processor")
   $69AD: RTS
   $69AE: CLC
   $69AF: RTS
   ```
3. When no second processor is detected (`X == 0`), `$.Boot` aborts with:
   `No 2nd Processor`
4. The main game file `$.Game` is **41,834 bytes**, which exceeds the total 32KB RAM of a standard unexpanded BBC Micro Model B.
   The 2023 anniversary edition was engineered specifically for the 64KB RAM space of the **Acorn 6502 Second Processor (Tube co-processor)**.
5. In `$.Boot`, the code transmits a bootstrap client from `$6057-$628D` across Tube Register 3 (`$FEE5`) to execute inside the second processor, while the host BBC Micro runs display and I/O services at `$2373`.

---

## 5. CAPTURED SCREENSHOT EVIDENCE

Authentic screenshots rendered directly from our emulated BBC Micro screen memory using the official Acorn font and Mode 7 Teletext attribute rendering:

### 1. DFS 0.90 Directory Listing (`*CAT`):
![Chuckie Egg Catalogue](screenshots/chuckie_egg_cat.png)

```text
BBC Computer 32K
Acorn DFS
BASIC
>*CAT
ChuckieEgg23 (00)
Drive 0             Option 3 (EXEC)
Directory :0.$      Library :0.$

    !Boot    L          Boot     L
    EditDat             Game     L
    UserDat
>
```

### 2. Chuckie Egg 2023 Loaded Title Banner:
![Chuckie Egg Title](screenshots/chuckie_egg_title.png)

```text
┌────────────────────────────────────────┐
│  x|||||||||||||||||||||||||||||||||t   │
│   !                               "    │
│           samskivington.com            │
│           samskivington.com            │
│               PRESENTS                 │
│   0                               `    │
│  +/////////////////////////////////'   │
│                                        │
│           Chuckie Egg 2023             │
│       40th Anniversary Edition         │
│                                        │
│  dgEVAL                                │
│ >                                      │
└────────────────────────────────────────┘
```

---

## 6. VERIFICATION & RUNNABLE COMMANDS

### Automated Tests:
All 10 unit tests pass cleanly:
```powershell
dotnet test
```
- `Test_RomInventoryHashes`: Verifies MOS 1.20, BASIC II, DFS 0.90 MD5/SHA.
- `Test_FloppyDrive_SsdLoading`: Verifies SSD geometry, sector decoding, catalogue parsing.
- `Test_Dfs_StarCat`: Verifies genuine DFS 0.90 `*CAT` execution via 8271 NMI.
- `Test_Dfs_RunBoot`: Verifies autoboot execution, sector streaming, `Boot` loading at `&6000`, Mode 7 title display.

### Runnable CLI Commands:
To boot and run Chuckie Egg from the SSD:
```powershell
dotnet run --project src/BbcMicro.Emulator -- --disk games/Disc999-chuckieEgg2023Web.ssd --autoboot
```

To run with live FDC register tracing:
```powershell
dotnet run --project src/BbcMicro.Emulator -- --disk games/Disc999-chuckieEgg2023Web.ssd --autoboot --trace-fdc
```

---

## 7. REMAINING ARCHITECTURAL WORK FOR 100% GAMEPLAY

To achieve full interactive gameplay with `ChuckieEgg23`:
1. **Acorn Tube ULA (`$FEE0-$FEF7`) Emulation:**
   - Emulate the 4 Tube register pairs (Control, OSWRCH, FIFO, Interrupt) between Host and Co-processor.
2. **Second 6502 CPU Co-Processor:**
   - Instantiate a second `Cpu6502` running with 64KB dedicated RAM (`$0000-$FFFF`), executing `$.Game`.
3. **Alternative Target:**
   - Also support the original 1983 32KB single-CPU BBC Micro Model B release of Chuckie Egg (which runs entirely in native Model B RAM without Tube).
