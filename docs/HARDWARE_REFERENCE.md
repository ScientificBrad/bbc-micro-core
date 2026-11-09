# Acorn BBC Micro Model B Hardware Reference

---

## 1. Memory-Mapped I/O Register Map ($FE00-$FEFF)

The BBC Micro Model B reserves the page `$FE00-$FEFF` for memory-mapped hardware controllers:

| Address Range | Device | Primary Function |
| :--- | :--- | :--- |
| **`$FE00-$FE07`** | **Motorola 6845 CRTC** | Video timing, raster address generator, screen start pointer |
| **`$FE20-$FE21`** | **Ferranti Video ULA** | Video control register, 16-entry dynamic palette latch |
| **`$FE30-$FE33`** | **Paged ROM Select Latch** | Selects active 16 KB Sideways ROM bank in `$8000-$BFFF` |
| **`$FE40-$FE4F`** | **System 6522 VIA** | Keyboard matrix, IC32 addressable latch, SN76489 sound, timers |
| **`$FE60-$FE6F`** | **User 6522 VIA** | User Port, printer port, ADC interface, 1 MHz extension bus |
| **`$FE80-$FE87`** | **Intel 8271 FDC** | Floppy disk controller status, command, parameter, and data |
| **`$FEE0-$FEE7`** | **Ferranti Tube ULA** | High-speed FIFO interface to Second Processor (parasite) |

---

## 2. Motorola 6845 CRTC Registers

Accessed via address register at `$FE00` and data register at `$FE01`:

| Register | Name | Typical Value (Mode 7) | Typical Value (Mode 1/2) | Description |
| :---: | :--- | :---: | :---: | :--- |
| **R0** | Horizontal Total | 63 | 127 | Total characters per raster line |
| **R1** | Horizontal Displayed | 40 | 80 | Displayed characters per raster line |
| **R2** | Horizontal Sync Position | 49 | 98 | Character column where H-Sync begins |
| **R3** | Sync Widths | &24 | &28 | Horizontal and vertical sync pulse widths |
| **R4** | Vertical Total | 30 | 38 | Total character rows per frame |
| **R5** | Vertical Total Adjust | 2 | 0 | Scanline adjustment for exact 50 Hz frame rate |
| **R6** | Vertical Displayed | 25 | 32 | Displayed character rows per frame |
| **R7** | Vertical Sync Position | 28 | 34 | Character row where V-Sync begins |
| **R8** | Interlace & Skew | 0 | 0 | Non-interlaced display mode |
| **R9** | Maximum Scanline | 9 | 7 | Scanlines per character row (0-based) |
| **R12** | Screen Start High | &28 | &06 | High byte of display RAM start address |
| **R13** | Screen Start Low | &00 | &00 | Low byte of display RAM start address |

---

## 3. Ferranti Video ULA Registers

- **`$FE20` (Control Register):**
  - Bit 0: Master cursor control
  - Bits 1–2: Dot rate (01 = 16 MHz, 00 = 8 MHz)
  - Bit 3: Teletext Mode 7 select (1 = SAA5050 active)
  - Bits 4–7: Character clock division factors

- **`$FE21` (Palette Register):**
  - Bits 0–3: Physical color (EOR 7: inverted RGB)
  - Bits 4–7: Logical color index (0–15)

---

## 4. System 6522 VIA Registers & IC32 Latch

Mapped at `$FE40-$FE4F`:

| Register | Name | Function in BBC Micro |
| :--- | :--- | :--- |
| **`$FE40`** | **ORB / IRB** | Port B data lines: Keyboard row input, IC32 latch data |
| **`$FE41`** | **ORA / IRA** | Port A data lines: Slow data bus (SN76489 sound, keyboard col) |
| **`$FE42`** | **DDRB** | Port B Data Direction Register |
| **`$FE43`** | **DDRA** | Port A Data Direction Register |
| **`$FE44`** | **T1CL** | Timer 1 Counter Low Byte |
| **`$FE45`** | **T1CH** | Timer 1 Counter High Byte (100 Hz system interrupt clock) |
| **`$FE4D`** | **IFR** | Interrupt Flag Register (Bit 6: T1, Bit 1: CA1 V-Sync, Bit 0: CA2 KBD) |
| **`$FE4E`** | **IER** | Interrupt Enable Register |

### IC32 Addressable Latch (Controlled via Port B Bits 0–3)
- **Output 0:** SN76489 Sound chip write enable (`/WE`, active low)
- **Output 1:** Speech processor read/write select
- **Output 2:** Keyboard auto-scan enable
- **Output 3:** Caps Lock LED
- **Output 4:** Shift Lock LED

---

## 5. Texas Instruments SN76489 Sound Register Map

Controlled by writing 8-bit data bytes to `$FE41` with IC32 Output 0 pulled low:

### Control Byte Format (`Bit 7 = 1`)
$$\text{Byte} = 1 \mid \text{Channel (2 bits)} \mid \text{Type (1 bit: 0=Freq, 1=Atten)} \mid \text{Data (4 bits)}$$

- **Channels:** `00` = Tone 1, `01` = Tone 2, `10` = Tone 3, `11` = Noise
- **Attenuation:** 4-bit value ($0000 = \text{Maximum Volume (0 dB)}$, $1111 = \text{Silence}$)

### Frequency Data Byte Format (`Bit 7 = 0`)
$$\text{Byte} = 0 \mid 0 \mid \text{Data High 6 bits}$$
Combines with lower 4 bits to form the 10-bit divider:
$$f_{\text{out}} = \frac{4{,}000{,}000}{32 \times \text{Divider}}$$

---

## 6. Intel 8271 Floppy Disk Controller Registers

Mapped at `$FE80-$FE84`:

| Address | Read Operation | Write Operation |
| :--- | :--- | :--- |
| **`$FE80`** | Status Register | Command Register |
| **`$FE81`** | Result Register | Parameter Register |
| **`$FE82`** | Status Register | Reset Register |
| **`$FE84`** | Read Data (DRQ) | Write Data (DRQ) |

### Status Register Bits
- Bit 0: Command Busy
- Bit 1: Command Full
- Bit 2: Parameter Full
- Bit 3: Result Full
- Bit 4: Interrupt Active
- Bit 7: Non-Maskable Interrupt (DRQ Data Transfer Ready)
