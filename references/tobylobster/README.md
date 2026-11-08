# TOBY NELSON'S MOS 1.20 ANNOTATED REASSEMBLY

**Author:** Toby Nelson (2020)  
**Primary Reference Web:** https://tobylobster.github.io/mos/  
**Source File:** `os120_acme.a` (1,156,583 bytes)  
**Derived Build:** `os120.bin` (MD5: `0a59a5ba15fe8557b5f7fee32bbd393a`)

---

## 1. Provenance & Attribution

This directory contains Toby Nelson's monumental annotated reassembly of Acorn MOS 1.20 for the BBC Microcomputer Model B.
As credited by Toby Nelson, this work builds upon:
- Geoff Cox's earlier annotated disassembly (Micronet, 1991).
- Graham Nelson (Inweb system layout and editorial review).
- Adrian Nelson (corrections and technical suggestions).
- The original Acorn Computers MOS design team: Sophie (Roger) Wilson, Steve Furber, Peter Miller, Graham Tebby, Chris Turner, Paul Bond, and Hermann Hauser.

## 2. Research Use & License Policy

Per Project 003 Research Guidelines:
- This material is retained strictly in `references/tobylobster/` as a research and verification reference.
- It is NOT part of our distributable emulator source code.
- All emulator code in `src/` is an independent, clean implementation developed by ScientificBrad.
- Any citations or comparative findings are credited to Toby Nelson.

## 3. Chapter Overview

- **Chapter 1:** Introduction and architecture.
- **Chapter 2:** Memory Layout and Constants.
- **Chapter 3:** Memory-Mapped I/O (FRED, JIM, SHEILA).
- **Chapter 4:** Character definitions and VDU tables (`$C000-$C4BF`).
- **Chapter 5:** VDU text driver and OSWORD routines (`$C4C0-$CB1C`).
- **Chapter 6:** Changing Video Modes and CRT controller setup (`$CB1D-$CCF7`).
- **Chapter 7:** Graphics cursor and user-defined characters (`$CCF8-$D05F`).
- **Chapter 8:** PLOT and 2D graphics primitive engine (`$D060-$D7C1`).
- **Chapter 9:** Cursor editing mode (`$D7C2-$D93F`).
- **Chapter 10:** Reset routines, default vectors, variable initialization (`$D940-$DC1B`).
- **Chapter 11:** Interrupt processing (IRQ1, IRQ2, BRK) (`$DC1C-$DEC4`).
- **Chapter 12:** Command Line Interpreter (CLI / Star commands) (`$DEC5-$E0A3`).
- **Chapter 13:** Character output (`OSWRCH`), printer routines, ring buffers (`$E0A4-$E20D`).
- **Chapter 14:** Star commands (`*LOAD`, `*SAVE`, `*KEY`, `*FX`) (`$E20E-$E5B2`).
- **Chapter 15:** OSBYTE and OSWORD dispatchers (`$E5B3-$EB02`).
- **Chapter 16:** Sound and Speech subsystems (SN76489 / TMS5220) (`$EB03-$EED9`).
- **Chapter 17:** Keyboard matrix scanning and keyboard tables (`$EEDA-$F134`).
- **Chapter 18:** Tape and ROM Filing Systems (CFS / RFS) (`$F135-$F587`).
- **Chapter 19:** Low-level cassette tape operations (6850 ACIA) (`$F588-$F8A8`).
- **Chapter 20:** Filing system messaging and error reporting (`$F8A9-$FAE7`).
- **Chapter 21:** Cassette hardware control routines (`$FAE8-$FBFF`).
- **Chapter 22:** Easter Egg contributor credits string (`$FC00-$FEFF`).
- **Chapter 23:** Extended vector entry points and dispatcher (`$FF00-$FFA6`).
- **Chapter 24:** Operating System API Jump Table and 6502 hardware vectors (`$FFA7-$FFFF`).
