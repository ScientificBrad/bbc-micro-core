# REA REPORT 06: ACORN TUBE SECOND PROCESSOR RECONSTRUCTION & CHUCKIE EGG COMPATIBILITY

**Researcher:** ScientificBrad  
**Series:** Historical Computer Reverse Engineering  
**Project:** 003: Acorn BBC Micro Model B (1981)  
**Target Architecture:** Acorn 6502/65C02 Second Processor ("Cheese Wedge" / Master Turbo)  
**Host Firmware:** Acorn MOS 1.20 (`$C000-$FFFF`), Acorn DFS 0.90 (`$8000-$9FFF`), Intel 8271 FDC (`$FE80-$FE84`)  
**Parasite System:** 65C02 CPU @ 3.0 MHz, 64 KB local RAM, Ferranti Tube ULA (`$FEE0-$FEE7` Host / `$FEF8-$FEFF` Parasite)  
**Target Software:** *Chuckie Egg 2023 (40th Anniversary Edition)* [co-pro] & *Chuckie Egg (1983 Original Model B Edition)*  

---

## 1. EXECUTIVE STATUS & MILESTONE MATRIX

| Component / Phase | Status | Verified Evidence |
|---|---|---|
| **65C02 Parasite Core** | **PASS** | CMOS instructions (STZ, BRA, PHX, PLX, PHY, PLY, zero-page indirect) |
| **Ferranti Tube ULA** | **PASS** | 4 bidirectional channels (R1, R2, R3, R4), authentic bit-set/bit-clear control |
| **MOS Tube Detection** | **PASS** | Hardware probe at `$DB38` (`STA $FEE0`, `ROR $FEE0`) & `OSBYTE &EA` ($027A) |
| **Dual-CPU Interleaving** | **PASS** | Deterministic 2 MHz Host / 3 MHz Parasite interleaved cycle scheduler |
| **Edge-Triggered NMI** | **PASS** | Authentic 6502 falling-edge NMI latch prevents infinite interrupt traps |
| **Chuckie Egg 2023 Bootstrap** | **PASS** | R4 Type 1 transfer, 566-byte R3 stream, Command 4 release at `$0800` |
| **Game Data Streaming** | **PASS** | Host DFS opens `$.Game` (41,834 bytes), displays Mode 7 "Loading" progress bar |
| **Parasite Decompression** | **PASS** | Parasite code executes at `$0800`, unpacks upper RAM, vectors IRQ to `$FE54` |
| **Host Tube Driver Block** | **BLOCKED (ROM)** | DFS 0.90 lacks Tube driver (requires DNFS ROM) to handle R4 Service Call 5 |
| **1983 Original Model B Fallback** | **PASS / PLAYABLE** | `Disc002-ChuckieEgg.ssd` `*RUN CH_EGG` executes 1983 engine, switches Mode 1 |

---

## 2. THE ACORN TUBE ARCHITECTURE

The Acorn Tube is a high-speed multiprocessor bus and protocol designed by Acorn Computers in 1981-1983 to offload applications, language compilers, and compute workloads to an external second processor, leaving the BBC Micro Model B as an I/O processor (IOP) managing screen memory, sound, keyboard, and disk filing.

### Hardware Topography:
- **Host Processor:** MOS Technology 6502 @ 2.0 MHz, 32 KB RAM, Acorn MOS 1.20 in ROM socket 0.
- **Parasite Processor:** Rockwell / WDC 65C02 @ 3.0 MHz, 64 KB dedicated dynamic RAM.
- **Interconnect:** Custom Ferranti Tube ULA containing four asynchronous FIFO channels and shared control registers.

```text
+-----------------------+                         +-----------------------+
|    BBC Micro Host     |                         |   Second Processor    |
|   6502 CPU @ 2 MHz    |                         |  65C02 CPU @ 3 MHz    |
|      32 KB RAM        |                         |      64 KB RAM        |
+-----------+-----------+                         +-----------+-----------+
            |                                                 |
            | Memory-Mapped                                   | Memory-Mapped
            | $FEE0 - $FEE7                                   | $FEF8 - $FEFF
            v                                                 v
+-------------------------------------------------------------------------+
|                           FERRANTI TUBE ULA                             |
|                                                                         |
| Channel 1 (R1): 24-byte Host->Parasite FIFO, 1-byte Parasite->Host latch |
| Channel 2 (R2): 1-byte Host->Parasite latch, 1-byte Parasite->Host latch |
| Channel 3 (R3): 24-byte Host->Parasite FIFO, 24-byte Parasite->Host FIFO|
| Channel 4 (R4): 1-byte Host->Parasite latch, 1-byte Parasite->Host latch |
|                                                                         |
| Control Register: 7-bit (bit-set/bit-clear addressing via MSB)          |
+-------------------------------------------------------------------------+
```

---

## 3. FERRANTI TUBE ULA REGISTER LOGIC & PROTOCOL

Reverse-engineering the Tube ULA (confirmed against David Banks' `hoglet67/ReTuLaReMake` Verilog reconstruction) reveals the authentic register specification:

### 3.1 Control Register Protocol (`$FEE0` Write)
The Tube ULA control register is 7 bits (`control[6:0]`). Writes use bit 7 (`value & 0x80`) as an operation selector:
- If **Bit 7 is 1**: Perform **bit-set** (`control |= value & 0x7F`).
- If **Bit 7 is 0**: Perform **bit-clear** (`control &= ~(value & 0x7F)`).

| Bit | Identifier | Hardware Function |
|---|---|---|
| 6 | `t_flag` | **Tube FIFO Reset:** 1 holds all internal FIFOs in reset. |
| 5 | `p_flag` | **Parasite Reset:** 1 asserts `p_rst_b` to reset parasite CPU. |
| 4 | `v_flag` | **Two-Byte Mode:** Enables two-byte packing on Channel 3. |
| 3 | `m_flag` | **Parasite NMI Enable:** Enables NMI from R3 data/space available. |
| 2 | `j_flag` | **Parasite IRQ Enable:** Enables IRQ from R4 data available (`hp4_dav`). |
| 1 | `i_flag` | **Parasite IRQ Enable:** Enables IRQ from R1 data available (`hp1_dav`). |
| 0 | `q_flag` | **Host IRQ Enable:** Enables Host IRQ from R4 data available (`ph4_dav`). |

### 3.2 Status Register Multiplexer
Both Host (`$FEE0-$FEE7`) and Parasite (`$FEF8-$FEFF`) status registers report:
- **Bit 7:** Read Ready (data available in receive FIFO/latch).
- **Bit 6:** Write Ready (space available in transmit FIFO/latch).
- **Bits 5..0:**
  - For Channel 1 (R1): returns `control[5:0]`.
  - For Channels 2, 3, 4: returns `6'b111111` (`0x3F`).

### 3.3 MOS 1.20 Hardware Detection Sequence (`$DB38`)
Acorn MOS 1.20 performs the following probe at cold boot:
```assembly
DB38: LDA #$81     ; Set q_flag (Bit 0) and command bit (Bit 7)
DB3A: STA $FEE0    ; Write to Tube Control Register
DB3D: LDA $FEE0    ; Read Host R1 Status
DB40: ROR A        ; Rotate Bit 0 into Carry flag
DB41: BCC $DB4D    ; If Carry == 0, Tube hardware is absent -> branch
DB43: ...          ; Tube detected! Initialize coprocessor link
```
Because Bit 0 of `$FEE0` returns `control[0]` (`q_flag`), setting Bit 0 results in Bit 0 reading back as 1, successfully passing the hardware probe.

---

## 4. CHUCKIE EGG 2023 TUBE BOOTSTRAP TRACE

Analysis of `games/Disc999-chuckieEgg2023Web.ssd` demonstrates that the 2023 40th Anniversary edition is an authentic Acorn Tube coprocessor game.

### 4.1 Probe & Handshake
1. **Machine Version Probe (`$692B`):** Invokes `OSBYTE 129` (probe machine type, returns `X=0xFF` for BBC Model B) and `OSBYTE 0` (probe OS version, returns `X=1` for MOS 1.20).
2. **Tube Detection (`$699F`):** Invokes `OSBYTE &EA` (234), which queries MOS variable `$0190 + &EA = $027A` (Tube presence byte).
3. **BRKV Installation (`$69C5`):** Copies a Break interceptor from `$6021` to `$0120` and sets `BRKV` (`$0202`) to `$0120`.

### 4.2 Tube Stream Protocol (`$69ED`)
At `$69ED`, `$.Boot` directly operates the Tube ULA to boot the second processor:
```assembly
69ED: A9 18       LDA #$18       ; Clear bits 4 & 3 (v_flag, m_flag)
69EF: 8D E0 FE    STA $FEE0
69F2: A9 8C       LDA #$8C       ; Set bits 3 & 2 (m_flag, j_flag)
69F4: 8D E0 FE    STA $FEE0
69F7: A9 01       LDA #$01       ; Command 1: Transfer Type 1 (load via R3)
69F9: 20 C2 6B    JSR $6BC2      ; Write to R4
69FC: A9 D2       LDA #$D2       ; Synchronization byte
69FE: 20 C2 6B    JSR $6BC2      ; Write to R4
6A01: 20 7D 6A    JSR $6A7D      ; Send 4-byte destination address ($00000800)
6A04: 20 C2 6B    JSR $6BC2      ; Send 5th byte ($00)
```
- **R3 Streaming:** Host streams exactly 566 bytes (`$6057-$628D`) into Tube R3 (`$FEE5`). The parasite drains R3 into `$0800-$0A36`.
- **Command 4 (Release):** Host sends Command 4 (`0x04`) with sync `0xD2` and address `$0800`. Parasite sends acknowledge to R4 and executes `JMP $0800`.
- **R1 Stream:** Host streams 702 bytes (`$628D-$654B`) into R1 (`$FEE1`).
- **Disk File Open:** Host issues `OSFIND` for `$.Game` and begins streaming sectors, displaying the Mode 7 "Loading" bar.
- **Parasite Execution:** Code at `$0800` unpacks the R1 payload into upper RAM, configures IRQ vector `$FFFE` to `$FE54` and NMI `$FFFA` to `$FE65`, then writes `0x00` to R4.

---

## 5. BLOCKER AUDIT & ROM DEPENDENCY

Per Phase 6 requirements, we audit the ROM requirements honestly:

1. **Host Side Tube Support:**
   - Standalone `dfs09.rom` (Acorn DFS 0.90) contains disk filing routines for the 8271 controller, but does **not** contain Tube host communication services.
   - On genuine hardware, running a Tube second processor with floppy disks requires **Acorn DNFS** (Disc & Network Filing System, ROM ID `DNFS 1.20` or `DNFS 1.30`), which intercepts Tube interrupts (Service Call 5) and routes OS calls between Host and Parasite.
   - When Parasite writes to R4, Tube ULA asserts `HostIrq`. In the absence of a Tube host driver (DNFS), MOS 1.20 broadcasts Service Call 5, which DFS 0.90 ignores, stalling host-parasite communication.
2. **Parasite Side Tube Support:**
   - A complete second processor environment requires the **Acorn 6502 Tube Client ROM** (2 KB boot ROM containing the Parasite MOS API dispatch table).
   - In accordance with Phase 6 instructions, missing proprietary ROMs are not downloaded or redistributed without authorization.

---

## 6. FALLBACK MILESTONE: ORIGINAL 1983 MODEL B CHUCKIE EGG

As mandated by Phase 6 Requirement 8, the original 1983 Acorn BBC Micro Model B edition of *Chuckie Egg* (A&F Software, 1983) was tested from `games/Disc002-ChuckieEgg.ssd`.

### 6.1 Catalogue Analysis
```text
Disk Title: CHUCKIE
File 0: $.CH_EGG  Load=1100 Exec=29AB Len=2700 (9,984 bytes)
File 1: $.CHUCKIE Load=1900 Exec=8023 Len=0DD0 (3,536 bytes)
File 2: $.!BOOT   Load=0000 Exec=0000 Len=0030 (48 bytes)
```

### 6.2 Execution Verification (`*RUN CH_EGG`)
- Command `*RUN CH_EGG` issued to DFS 0.90.
- Intel 8271 reads 39 sectors from track boundary, streaming 9,984 bytes into RAM at `$1100-$37FF`.
- CPU vectors directly to execution entry point `$29AB`.
- Game engine sets up zero-page vectors, switches to Mode 1 bitmap graphics (`VDU 22, 1`), clears screen memory (`$3000-$7FFF`), and displays "Get Ready" and "Player 1".
- Verified in automated test `Test_ChuckieEgg1983_ModelB_Execution`.
- Screenshot rendered via `ScreenFramebufferRenderer.RenderMode1` and saved to `screenshots/chuckie_egg_1983.bmp` and `screenshots/chuckie_egg_1983.png`.

---

## 7. AUTOMATED TEST SUITE STATUS

All 21 automated regression and unit tests in `BbcMicro.Tests` pass with zero failures:

```text
Passed!  - Failed: 0, Passed: 21, Skipped: 0, Total: 21, Duration: 1 s
```

Key test coverage:
- `Test_TubeUla_Channel1_FifoAndLatching`: R1 24-byte FIFO depth, status flags, and latching.
- `Test_TubeUla_Channel3_FifoAndControlReg`: R3 bidirectional FIFO, control register bit-set/bit-clear.
- `Test_TubeUla_Channel2_And_Channel4_Latches`: R2 & R4 command/data passing.
- `Test_Tube_Mos120_Detection`: MOS 1.20 Tube probe at `$DB38` and `OSBYTE &EA`.
- `Test_Cpu65C02_Extensions`: CMOS instructions (STZ, BRA, PHX/PLX, PHY/PLY, zero-page indirect).
- `Test_DualCpu_InterleavedExecution`: 2 MHz Host / 3 MHz Parasite lockstep interleaving.
- `Test_ChuckieEgg2023_TubeHandshakeAndTransfer`: 566-byte R3 bootstrap, 702-byte R1 payload, OSFIND `Game`.
- `Test_ChuckieEgg1983_ModelB_Execution`: Full DFS sector streaming, entry at `$29AB`, Mode 1 switch.
