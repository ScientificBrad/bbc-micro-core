# PROJECT 003 — PHASE 6: ACTUAL ACORN TUBE 6502 SECOND PROCESSOR

Workspace: `D:\Emulation\BBCMicro`
Target game: `games\Disc999-chuckieEgg2023Web.ssd`
Goal: achieve real Chuckie Egg 2023 **INTERACTIVE** and **PLAYABLE** status, with evidence, using OUR emulator.

## Verified baseline

- Original MOS 1.20 / BASIC II / DFS 0.90 boots. Intel 8271 SSD sector read and DFS `*CAT` work.
- Supplied Chuckie Egg 2023 edition loads `$.Boot` into host at &6000 and draws anniversary title.
- `$.Boot` probes Tube presence (report disassembly: OSBYTE &EA at &699F), fails without second processor. `$.Game` ~41,834 bytes.
- Current `dotnet test --no-restore` passed 10/10 (Nov 8 2026).
- Existing uncommitted changes (including Matrix demo) must be preserved. Inspect `git status` first; do not reset or overwrite.
- Read `REA/05_CHUCKIE_EGG_COMPATIBILITY.md`, `REA/FINDINGS_LEDGER.md`, and inspect actual binary disassembly to validate claims rather than trusting previous notes blindly.

## CRITICAL RESEARCH FINDING

Acorn's external second processor is a **3 MHz 65C02-family** system with **64 KB local RAM**, **Tube ULA** and **Tube client boot firmware**. This is NOT just a second instance of the existing NMOS 6502 interpreter. Its host requires suitable Tube host-side ROM code (typically **DNFS**, not necessarily the standalone `dfs09.rom`). With stock MOS + DFS 0.90 and dummy Tube hardware, OSBYTE detection alone does not constitute a functional Tube system.

References:
- Acorn second processor service manual: https://www.microcomputer.org.uk/documents/manuals/second_processors/6502_processor_service.pdf
- Tube client disassembly and protocol: https://acornaeology.uk/acorn-6502-tube-client/
- Tube channels and register overview: https://acornaeology.uk/acorn-6502-tube-client/glossary.html
- Chuckie Egg 2023 is explicitly marked [co-pro]: https://bbcmicro.co.uk/game.php?id=4426

## Implementation sequence and deliverables

1. **Audit available ROMs and licensing.** List files under `roms/`; identify Tube host DNFS ROM and parasite Tube client ROM requirements by hashes, provenance and known vectors. Do not mislabel DFS 0.90 as DNFS; do not claim success with absent proprietary firmware. Support `--dnfs-rom <path>` and `--tube-rom <path>` (or equivalent), with clear missing-file errors. Do not download or redistribute proprietary ROMs without authorization. If unavailable, implement ROM-free Tube protocol unit tests and stop at an honestly labelled milestone.

2. **65C02 core correctness.** Reuse existing CPU infrastructure only where ISA-compatible; implement/test CMOS-specific opcodes, flags, zero-page indirect addressing, interrupt and WAI/STP semantics as required. Preserve existing NMOS 6502 behaviour and tests. Separate parasite bus (64 KB RAM, boot ROM memory overlay, parasite Tube register mapping) from host bus.

3. **Real Tube ULA**. Implement four bidirectional register channels R1–R4 with actual ready/full status, FIFO depths, shared-state control flags, interrupts and NMI to correct processor, resets and backpressure. Host addresses &FEE0–&FEE7 (mirroring if necessary); parasite addresses and boot ROM window must follow actual hardware and firmware docs. Never fake OSBYTE &EA return or invent parasitic transfers.

4. **Dual-CPU scheduler.** Run host 2 MHz and parasite 3 MHz, with deterministic interleaving, IRQ/NMI delivery and reset sequencing, and stable tube round-trip tests. Prevent clock drift / lockups. Add debug trace `--trace-tube` with bounded logs.

5. **Boot the real Tube client**: verify parasite starts at actual firmware reset vector, transfers client code into upper RAM according to hardware boot behaviour, initializes host/parasite handshakes, and responds to MOS OSBYTE/OSWORD/OSWRCH/OSCLI calls via host service. Track binary PCs, memory and FIFO bytes in logs.

6. **Play the supplied Chuckie Egg**: preserve disk-based DFS loader. Read `!Boot` and `Boot` from SSD, use Tube protocol to transfer `$.Game` to 64 KB parasite memory, then execute actual game code. Confirm host graphics requests, input and gameplay progression. Implement needed framebuffer Mode 0–6 and Mode 7 faithfully from BBC RAM/Video ULA/6845, plus keyboard matrix and only then polish window UI; report gaps in sound. Screenshot actual emulated frames, never mock up.

7. **Regression and documentation**: `dotnet test` all original and new tests; add ROM-free Tube unit tests, 65C02 instruction tests, transfer tests, boot ROM handshake tests and a Chuckie Egg trace with watchdog. Create `REA/06_TUBE_SECOND_PROCESSOR.md`, `docs/TUBE_IMPLEMENTATION.md` and update `README.md` with exact executable flags and honest statuses: TUBE INIT / GAME TRANSFER / GAME EXEC / GRAPHICS / INPUT / PLAYABLE.

8. **Fallback objective**: If no legal Tube ROM can be used or implementation is blocked, document exact missing assets and blockers, then use a separate independently owned 1983 Model B Chuckie Egg SSD as an alternative single-CPU compatibility target. Do not rename this 2023 2nd-processor edition as the 1983 original.

## Acceptance

No hardcoded title/gameplay, no simulated pass from presence bit, no gameplay drawn by host code instead of CPU. The run is complete only when original game bytes are loaded via 8271/DFS and Tube and the user can control a real frame-updating game window. If not done, supply working changes, failing first PC/register/command, tests, and clear next action. Do not erase the work already achieved.
