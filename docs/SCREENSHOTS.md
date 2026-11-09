# Visual Evidence & Screenshot Gallery

This document catalogs the visual verification screenshots captured directly from the emulator's rendering pipeline.

---

## 1. Gallery Catalog

| Figure | Image File | Display Mode | Resolution | Software / Scene | Key Verification Features |
| :---: | :--- | :---: | :---: | :--- | :--- |
| **1** | `01_bbc_boot.png` | MODE 7 | 640×500 | Genuine Acorn MOS 1.20 | Teletext SAA5050 character generator, cold boot banner, DFS 0.90 recognition, BASIC prompt. |
| **2** | `02_chuckie_egg_title.png` | MODE 2 | 640×512 | Chuckie Egg (1983) | 16-color Mode 2 rendering, giant caged mother duck, title banner, high-contrast palette. |
| **3** | `03_chuckie_egg_gameplay.png`| MODE 2 | 640×512 | Chuckie Egg (1983) | Live gameplay: Harry climbing ladder, seed corn, bonus timer at 800, score 000200, duck AI. |
| **4** | `04_elite_flight_controls.png`| MODE 7 | 640×500 | Elite (1984) | Flight controls briefing scroll, colored Teletext control codes (Red f0/f4/f6, Yellow, Blue). |
| **5** | `05_snapper_instructions.png`| MODE 7 | 640×500 | Snapper (1982) | Mode 7 instruction screen, Acornsoft copyright, space-bar latching via System VIA CA2. |
| **6** | `06_strip_poker_warning.png` | MODE 1 | 640×512 | Strip Poker (1984) | Mode 1 high-resolution warning triangle (Red/White/Black), pixel-accurate 2bpp decoding. |
| **7** | `07_zelda_bugeyes.png` | MODE 7 | 640×500 | Zelda Meets The Bug-Eyes (1983) | Teletext game screen, Riverdale Publishing header, alphanumeric text rendering. |
| **8** | `08_exile_title.png` | MODE 7 | 640×500 | Exile (1988) | Mode 7 briefing ("The planet Phoebus has been taken over..."), Superior Software banner. |

---

## 2. Technical Rendering Parameters

All images are exported directly from `BbcMachine` RAM using `ScreenFramebufferRenderer`:
- **Mode 7:** Emulates Mullard SAA5050 character generation using Acorn MOS 1.20 internal font glyphs at `$C000-$C2FF` scaled $2\times$ horizontally and vertically ($40 \text{ cols} \times 8 \times 2 = 640 \text{ pixels}$, $25 \text{ rows} \times 10 \times 2 = 500 \text{ pixels}$).
- **Modes 1 & 2:** Decodes 8-scanline character blocks from `$3000-$7FFF` scaled to $640 \times 512$ with physical palette lookups.
