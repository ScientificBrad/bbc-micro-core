# Third-Party Media & Intellectual Property Audit

---

## 1. Clean-Room Distribution Policy

This repository is maintained as a **clean-room reverse engineering and architectural preservation project**.

- **No Proprietary ROM Dumps in Git:** Acorn MOS 1.20, BBC BASIC II, and Acorn DFS 0.90 are proprietary firmware owned by the respective copyright successors. They are excluded from git tracking via `.gitignore` and must be provided by the user from legally acquired hardware.
- **No Commercial Disk Dumps in Git:** Commercial disk images (`.ssd`, `.dsd`) are excluded from repository distribution.
- **Automated CI Validation:** Unit and architectural test suites run in GitHub Actions using synthetic clean-room test vectors without copyrighted ROM dumps.

---

## 2. Firmware ROM Inventory

| Filename | Title | Publisher / Rights Holder | Year | Size | SHA-256 Digest | Status |
| :--- | :--- | :--- | :---: | :---: | :--- | :---: |
| `os12.rom` | Acorn MOS 1.20 | Acorn Computers Ltd | 1982 | 16,384 | `4e12e105eefb925b34003d19dd4ae1ad6f32e2c56a8775440d99ef83a30fc3cb` | User Provided |
| `basic2.rom`| BBC BASIC II | Acorn Computers Ltd | 1982 | 16,384 | `79493f1325b154a1d479e0a0583b482c892bc5c5cf25470d0615fbab4a63116a` | User Provided |
| `dfs09.rom` | Acorn DFS 0.90 | Acorn Computers Ltd | 1982 | 8,192 | `d3080db0945cb5f23bc327bc6c412f171221b6ee24ca7ddb8dbe94a50438cf48` | User Provided |

---

## 3. Commercial Disk Images Audit

The following software was evaluated strictly for historical compatibility verification and architectural benchmarking. Source: The Complete BBC Micro Games Archive (`https://bbcmicro.co.uk/`).

| Filename | Original Title | Publisher / Author | Year | Size | SHA-256 Digest | Preservation Rights |
| :--- | :--- | :--- | :---: | :---: | :--- | :---: |
| `Disc002-ChuckieEgg.ssd` | Chuckie Egg | A&F Software / Nigel Alderton | 1983 | 204,800 | `f044aa7d97d4c2ce64821869e591244033c467a364125f46e5ca932230113ddf` | Proprietary (Excluded) |
| `Disc021-EliteD.ssd` | Elite | Acornsoft / Braben & Bell | 1984 | 204,800 | `01934cbbfe7bdf416e95aaad0a394ec5fcce192231e67fa890e7845f06121b65` | Proprietary (Excluded) |
| `Disc001-SnapperV2.ssd` | Snapper | Acornsoft / Jonathan Griffiths | 1982 | 204,800 | `553e6bb69d6e2e505ea5bf451e0ea288229b46f5e2cb83a4505c879cff69d76f` | Proprietary (Excluded) |
| `Disc999-StripPoker.ssd` | Strip Poker | Anirog Software | 1984 | 155,136 | `dcba94017f7ba50e326b20f925b4ec2b3cf4bc534571dbbb6e053fa40366c858` | Proprietary (Excluded) |
| `Disc999-StripPoker2Plus.ssd`| Strip Poker II+ | Anirog Software | 1988 | 93,952 | `188da02daeb318f7a6a4c281df69b2d8aa982cf6dc2ec40e94bbba7372d622c1` | Proprietary (Excluded) |
| `DiscA02-ZeldaMeetsTheBugEyesAlt.ssd` | Zelda Meets The Bug-Eyes | Riverdale Publishing | 1983 | 204,800 | `db573f0c13bb101a8fb1d7634f19bca1b4a8e2cb932e604724bfa7ea71caaa74` | Proprietary (Excluded) |
| `Disc040-ExileR.ssd` | Exile | Superior Software / Irvin & Smith | 1988 | 204,800 | `f7d9121ec7a5622d87fe0c21469959c27d01f2518e7ede8af3fa9bc0889acda6` | Proprietary (Excluded) |
| `Disc001-ZanyKong.ssd` | Zany Kong | Superior Software | 1984 | 204,800 | `9ff81dbcf24898860228d712e0e4b7bdf0aa29e5e7e008fa37d8e64c399b119a` | Proprietary (Excluded) |
| `Disc025-SnapmanClassicArcadeGames.ssd` | Snapman | Micro Power | 1983 | 204,800 | `fcaeec2476d6541f6e2bceea987dc34c56ee256e2eb93e030a6c096414a38f32` | Proprietary (Excluded) |
| `Disc999-ReptonInfinity.ssd` | Repton Infinity | Superior Software | 1988 | 135,424 | `1f467554d3148a27d2c33215266a4a82a6134b9d0b67e2a9b3a0e6e737ae49e7` | Proprietary (Excluded) |
| `Disc999-chuckieEgg2023Web.ssd` | Chuckie Egg 2023 (Tube) | Daniel Finch | 2023 | 204,800 | `639abc21270e51c9a8c8dc84cb58ac955e5bc5e787395796c5e1f906dd39616f` | Freeware / Non-Commercial |
| `Disc999-StuntCarRacerSTD.ssd` | Stunt Car Racer | Bitshifters (Port) | 2019 | 127,488 | `69082f42a592750e33beae77764b85434cf95ea912eb8903c739a8cbe22bc3fc` | Non-Commercial Port |
