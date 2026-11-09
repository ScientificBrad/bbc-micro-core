# Commercial Game Compatibility Suite

---

## 1. Compatibility Overview & Methodology

To ensure authentic historical fidelity, the emulator is tested directly against original Acorn DFS floppy disk images (`.ssd`). All games boot via the genuine Acorn MOS 1.20 and DFS 0.90 firmware using `*EXEC !BOOT` or `*RUN`.

Software compatibility is classified under rigorous empirical criteria:

- **PLAYABLE:** Complete gameplay loop functional, interactive keyboard/joystick input responsive, audio synthesized correctly, graphics free of corruption.
- **PLAYABLE WITH ISSUES:** Game engine runs and accepts input, but minor timing quirks, unsupported peripheral calls, or audio edge-cases exist.
- **INTERACTIVE:** Boots through intro/title screens and accepts input on menus or instructions, but core gameplay loop is blocked.
- **TITLE:** Boots and displays title/intro screen, but does not advance to interactive menu.
- **BOOTS:** Machine code executes from disk, but crashes before rendering a visual title.
- **FAILED:** Incompatible due to explicit hardware prerequisites (e.g. requires BBC Master 128).

---

## 2. Compatibility Matrix

| Game Title | Release | Publisher | Disk Image | Video Mode | Status |
| :--- | :---: | :--- | :--- | :---: | :---: |
| **Chuckie Egg** | 1983 | A&F Software | `Disc002-ChuckieEgg.ssd` | MODE 2 | **PLAYABLE** |
| **Strip Poker** | 1984 | Anirog | `Disc999-StripPoker.ssd` | MODE 1 / 7 | **PLAYABLE** |
| **Strip Poker II+** | 1988 | Anirog | `Disc999-StripPoker2Plus.ssd` | MODE 1 / 2 | **PLAYABLE** |
| **Zelda Meets The Bug-Eyes**| 1983 | Riverdale | `DiscA02-ZeldaMeetsTheBugEyesAlt.ssd`| MODE 7 | **PLAYABLE** |
| **Zany Kong** | 1984 | Superior | `Disc001-ZanyKong.ssd` | MODE 2 | **PLAYABLE** |
| **Snapman** | 1983 | Micro Power | `Disc025-SnapmanClassicArcadeGames.ssd`| MODE 1 / 2 | **PLAYABLE** |
| **Elite** | 1984 | Acornsoft | `Disc021-EliteD.ssd` | MODE 7 / 1 | **INTERACTIVE** |
| **Snapper** | 1982 | Acornsoft | `Disc001-SnapperV2.ssd` | MODE 7 / 2 | **INTERACTIVE** |
| **Exile** | 1988 | Superior | `Disc040-ExileR.ssd` | MODE 7 / 2 | **INTERACTIVE** |
| **Repton Infinity** | 1988 | Superior | `Disc999-ReptonInfinity.ssd` | MODE 1 / 2 | **TITLE** |
| **Chuckie Egg 2023 (Tube)** | 2023 | Daniel Finch | `Disc999-chuckieEgg2023Web.ssd` | MODE 2 | **TITLE (TUBE BOOT)** |
| **Stunt Car Racer** | 2019 | Bitshifters | `Disc999-StuntCarRacerSTD.ssd` | MODE 5 | **FAILED (NEEDS MASTER)** |

---

## 3. Detailed Software Case Studies

### 3.1 Chuckie Egg (1983) — Nigel Alderton / A&F Software
- **Command:** `*RUN CH_EGG`
- **Configuration:** BBC Model B (32 KB RAM), MOS 1.20, DFS 0.90
- **Status:** **PLAYABLE** (Flagship Benchmark)
- **Observations:** Boots cleanly into Mode 2 high-color display. Pressing 'S' selects single player, '1' chooses keyboard controls. Hen-house platforms, ladders, seed piles, bouncing eggs, ducks, and the giant caged ostrich render pixel-perfect. Real-time SN76489 sound synthesis reproduces authentic footstep clicks, jump chirps, and egg collection fanfares.

### 3.2 Elite (1984) — David Braben & Ian Bell / Acornsoft
- **Command:** `*EXEC !BOOT`
- **Configuration:** BBC Model B, MOS 1.20, DFS 0.90
- **Status:** **INTERACTIVE**
- **Observations:** The disc loader displays the iconic Mode 7 Flight Navigation and Trading Controls instruction scroll. Pressing 'Y' accepts joystick setup and initiates the Cobra Mk III vector spaceflight engine.

### 3.3 Snapper (1982) — Jonathan Griffiths / Acornsoft
- **Command:** `*EXEC !BOOT`
- **Configuration:** BBC Model B, MOS 1.20, DFS 0.90
- **Status:** **INTERACTIVE**
- **Observations:** Loads Mode 7 briefing with game instructions and key controls. Keyboard matrix polling detects space-bar press cleanly via System VIA CA2 interrupt.

### 3.4 Strip Poker (1984) — Anirog Software
- **Command:** `*EXEC !BOOT`
- **Configuration:** BBC Model B, MOS 1.20, DFS 0.90
- **Status:** **PLAYABLE**
- **Observations:** Displays high-resolution Mode 1 warning screen ("THE FOLLOWING PROGRAM IS NOT FOR UNDER 16"). Pressing space advances into opponent selection and poker hand engine.

### 3.5 Chuckie Egg 2023 (Anniversary Tube Edition) — Daniel Finch
- **Command:** `*EXEC !BOOT`
- **Configuration:** BBC Model B with 65C02 Tube Second Processor
- **Status:** **TITLE (TUBE BOOT)**
- **Observations:** The host 6502 loads the bootstrap code into `$1900`, discovers the Tube ULA registers at `$FEE0-$FEE7`, transfers the parasite payload across FIFO Channel 1, releases the parasite reset line, and boots the 3 MHz 65C02 processor into the animated title screen. Active gameplay loops require full DNFS Tube OS call interception.

### 3.6 Stunt Car Racer (2019) — Bitshifters Port
- **Command:** `*EXEC !BOOT`
- **Status:** **FAILED (ARCHITECTURAL CONSTRAINT)**
- **Observations:** The 2019 Bitshifters port strictly requires a BBC Master 128 (65SC12 CPU, 128 KB RAM, sideways RAM in banks 4–7, and Hazel memory at `$CE00`). On a Model B, the bootloader identifies `OSBYTE 0` ($X < 3$) and correctly halts with `"BBC Master required!"`.
