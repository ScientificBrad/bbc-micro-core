# Automated Testing Suite & Verification Matrix

---

## 1. Test Architecture

The testing framework employs **.NET 8 xUnit** to ensure cycle-accurate micro-architectural fidelity, memory banking correctness, video rendering precision, audio synthesis consistency, and real disk image autoboot execution.

```
       +-------------------------------------------------------------------------------+
       |                      BBC MICRO AUTOMATED TEST HARNESS                         |
       +-------------------------------------------------------------------------------+
                                               |
         +-------------------------------------+-------------------------------------+
         |                                     |                                     |
  +---------------+                     +---------------+                     +---------------+
  | CPU & Memory  |                     |  Peripherals  |                     | Disc & Audio  |
  |  Unit Tests   |                     |  Unit Tests   |                     | System Tests  |
  +---------------+                     +---------------+                     +---------------+
  | MOS 6502 core |                     | Motorola 6845 |                     | Intel 8271 FDC|
  | Address modes |                     | Video ULA Mode|                     | DFS 0.90 boot |
  | Zero-page wrap|                     | Scanline stride|                    | SN76489 audio |
  | 32 KB RAM     |                     | Mode 7 SAA5050|                     | Game autoloader|
  +---------------+                     +---------------+                     +---------------+
```

---

## 2. Test Execution Breakdown

| Test Suite File | Subsystem Verified | Tests | Status |
| :--- | :--- | :---: | :---: |
| `FirmwareAndCpuTests.cs` | 6502 instructions, addressing modes, stack, MOS boot banner | 7 | **PASS** |
| `RamVerificationTests.cs` | 32 KB continuous RAM, zero-page wrapping, I/O space boundary | 4 | **PASS** |
| `Mode1FramebufferTests.cs` | CRTC timing, Video ULA palette, 8-scanline block interleaving | 6 | **PASS** |
| `Sn76489AudioTests.cs` | 3 tone channels, 15-bit noise LFSR, logarithmic volume levels | 4 | **PASS** |
| `TubeSecondProcessorTests.cs` | Ferranti Tube ULA 4 FIFOs, 65C02 parasite execution | 8 | **PASS** |
| `FloppyAndAutobootTests.cs` | Intel 8271 FDC, DFS 0.90, *CAT/*RUN, screenshot generator | 7 | **PASS** |
| **TOTAL** | **Comprehensive System Harness** | **36** | **36 / 36 (100%)** |

---

## 3. Running Automated Tests

To execute all tests with Release optimizations:

```powershell
dotnet test tests/BbcMicro.Tests -c Release
```
