# ANTIGRAVITY MISSION: BBC MICRO CHUCKIE EGG BOOT AND PLAY

You are working IN PLACE in the existing **Project 003: BBC Micro Model B** repository:

`D:\Emulation\BBCMicro`

## Goal

Boot and play the BBC Micro game from the authentic disc bytes using the actual Acorn MOS 1.20, BBC BASIC II, Acorn DFS 0.90 and our 6502 emulator. We need a live, interactive **game window** that accepts keyboard controls, displays game graphics, and advances the game simulation. No stand-in visuals, fake title screens, hand-written game recreation, or manually injected game status.

## Disc already on disk

`D:\Emulation\BBCMicro\games\Disc999-chuckieEgg2023Web.ssd`

Observed locally:
- 204,800 bytes, conventional 80-track single-sided SSD image
- DFS catalogue title: `ChuckieEgg23`
- DFS boot option: **3 (EXEC !BOOT)**
- File catalogue: `$.EditDat`, `$.UserDat`, `$.Game`, `$.Boot`, `$.!Boot`
- `$.!Boot` is exactly 7 bytes and contains `*/Boot\r`
- `$.Boot` has LOAD/EXEC `&6000` and length ~3066 bytes
- `$.Game` is ~41,834 bytes

Do not assume this exact 2023-labelled SSD is bitwise-identical to the original 1983 release. Analyse this supplied game image as-is.

## Project baseline

- `src/BbcMicro.Core/`: real 6502 core, memory bus, system/user VIAs, keyboard matrix and CRTC/video ULA.
- `src/BbcMicro.Emulator/`: .NET 8 console runner, boots original ROMs and injects BASIC commands.
- `tests/BbcMicro.Tests/`: ROM, CPU, boot and BASIC regression tests.
- `roms/os12.rom`, `basic2.rom`, `dfs09.rom` already present locally. Never redistribute these binaries.
- Baseline boot and immediate-mode `PRINT 40+2` have been verified locally.
- There is unfinished, uncommitted Matrix demonstration code in `games/MATRIX.BAS`, `src/BbcMicro.Core/Mode7AnsiRenderer.cs`, `src/BbcMicro.Emulator/MatrixDemo.cs`, and modified `Program.cs`. **Preserve this work. Do not revert or overwrite it without careful integration.** The Matrix runner is NOT yet a passing test and is not the priority of this mission.

## Required engineering sequence

1. **Inventory and diagnostics**: Record current git diff and baseline `dotnet test` status. Parse the DFS SSD catalogue correctly, including file names, lengths, load/exec addresses, start sectors, boot bits and file allocation. Verify hashes and inspect `!BOOT`, `Boot`, `Game` to understand load flow before implementing anything.

2. **Real DFS disk support**: Implement reusable SSD sector read and mount using standard 10 sectors/track x 256 bytes, with bounds checks. Build the appropriate emulated **Intel 8271 floppy controller** at $FE80-$FE9F, including the actual commands, status/parameters, interrupts/NMI and timing required by MOS/DFS to load this disk. Reproduce the genuine firmware's calls. Do NOT patch BBC BASIC, inject the game into RAM as the main solution, or spoof OSFILE/OSGBPB as success. Add traces for each command and detect unimplemented 8271 operations explicitly.

3. **Verified autoboot**: Provide command-line invocation such as
   `dotnet run --project src/BbcMicro.Emulator -- --disk games/Disc999-chuckieEgg2023Web.ssd --autoboot`
   Make the actual DFS boot option 3 execute `!BOOT`, then `*/Boot`, then the game's machine code under emulation. Prove this with PC/CPU traces and disk-read evidence. The proposed flags can be adjusted for architecture, but document exact runnable commands.

4. **Display & controls**: Implement a **real** window/framebuffer with BBC-compatible bitmap MODE display needed by this program (discover which mode the game requests, do not assume MODE 7). Render from emulated RAM under the actual CRTC and Video ULA registers; handle palette and frame pacing. Route real-time keyboard controls into the BBC keyboard matrix/VIA, not just pretyped command injection. Sound only if needed for the first playable milestone; document remaining gaps. Keep headless operation working.

5. **Acceptance/regression tests**: Validate known ROM SHA-256 and existing tests. Add disk parser tests, a deterministic SSD mount/sector test, DFS catalogue test, autoboot progression test, and game-visible framebuffer test. Include crash/hang watchdogs, trace first failing PC and disk command, and never mark PASS from simply seeing a boot prompt or displaying a hardcoded graphic.

6. **Evidence and reporting**: Produce `REA/05_CHUCKIE_EGG_COMPATIBILITY.md` with disk provenance/hash, controller implementation, loader trace, screenshots from the live framebuffer (if achieved), tests, exact command to play, remaining hardware issues, and honest status labels: BOOTS / LOADS / TITLE / INTERACTIVE / PLAYABLE. Update README and changelog only with independently verified functionality. Keep a checklist in a plain-text progress note.

## Priorities

Work on software code immediately, not only planning. Fix the *first blocking real hardware issue*, run the real game, iterate and verify. Do not create an unrelated emulator or import a different emulator as if it were ours. Do not hardcode Chuckie Egg's screen, keyboard responses or machine-code results. Existing ROM source/reassembly should guide accurate behaviours. Preserve Matrix changes. Avoid huge debug output; store traces as files and report first actionable fault. If end-to-end playable is not reached, leave runnable code and evidence of the highest genuine milestone, with no fabricated PASS.

**Definition of done**: the supplied SSD boots via real DFS into Chuckie Egg and the user can see and control the game in a running window. Anything short of that is a documented intermediate milestone, not completion.
