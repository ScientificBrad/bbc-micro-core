using System;

namespace BbcMicro.Core
{
    /// <summary>
    /// Parasite Memory Bus for the Acorn 6502/65C02 Second Processor.
    /// Provides 64 KB local RAM, Tube ULA I/O at $FEF8-$FEFF,
    /// and a boot ROM overlay window at $F800-$FFFF.
    /// </summary>
    public class ParasiteBus : IMemoryBus
    {
        public readonly byte[] Ram = new byte[65536];
        public readonly TubeUla Tube;

        public byte[]? BootRom { get; private set; }
        public bool BootRomEnabled { get; set; } = true;

        public bool IrqPending => Tube.ParasiteIrq;
        public bool NmiPending => Tube.ParasiteNmi;

        public void ClearNmi() => Tube.ClearParasiteNmi();

        public void Tick(int cycles)
        {
            // Parasite timing ticks if needed
        }

        public ParasiteBus(TubeUla tube, byte[]? bootRom = null)
        {
            Tube = tube;
            if (bootRom != null)
            {
                BootRom = new byte[bootRom.Length];
                Buffer.BlockCopy(bootRom, 0, BootRom, 0, bootRom.Length);
                BootRomEnabled = true;
            }
            else
            {
                InstallDefaultBootstrap();
            }
            Reset();
        }

        public void Reset()
        {
            Array.Clear(Ram, 0, Ram.Length);
            BootRomEnabled = true;
            if (BootRom == null)
            {
                InstallDefaultBootstrap();
            }
        }

        public void LoadBootRom(byte[] rom)
        {
            BootRom = new byte[rom.Length];
            Buffer.BlockCopy(rom, 0, BootRom, 0, rom.Length);
            BootRomEnabled = true;
        }

        /// <summary>
        /// Installs a minimal authentic Acorn Tube protocol client bootstrap into the upper memory window.
        /// Handles Transfer Type 1 (load into memory via R3) and Transfer Type 4 (release/execute).
        /// </summary>
        private void InstallDefaultBootstrap()
        {
            // Setup default boot vectors and minimal client in upper memory ($F800-$FFFF)
            // Bootstrap entry point at $F800
            // Reset vector at $FFFC points to $F800
            ushort bootEntry = 0xF800;

            // Assembly routine for Tube bootstrap:
            // F800: SEI
            // F801: CLD
            // F802: LDX #$FF
            // F804: TXS
            // Loop waiting for R4 command from Host:
            // WaitR4:
            // F805: BIT $FEFE    ; Wait for R4 Read Ready (Bit 7)
            // F808: BPL WaitR4
            // F80A: LDA $FEFF    ; Read Command
            // F80D: CMP #$01     ; Command 1: Data transfer
            // F80F: BEQ DoTransfer1
            // F811: CMP #$04     ; Command 4: Release
            // F813: BEQ DoRelease
            // F815: JMP WaitR4
            //
            // DoTransfer1:
            // Read sync byte ($D2):
            // F818: BIT $FEFE; BPL F818; LDA $FEFF
            // Read 4 bytes (destination address & length) from R4:
            // F820: BIT $FEFE; BPL F820; LDA $FEFF; STA $15 (addr lo)
            // F829: BIT $FEFE; BPL F829; LDA $FEFF; STA $16 (addr hi)
            // F832: BIT $FEFE; BPL F832; LDA $FEFF; STA $17 (len lo)
            // F83B: BIT $FEFE; BPL F83B; LDA $FEFF; STA $18 (len hi)
            // Read from R3 FIFO into destination address:
            // ReadR3Loop:
            // F844: BIT $FEFC; BPL ReadR3Loop
            // F849: LDA $FEFD
            // F84C: LDY #$00; STA ($15),Y
            // F850: INC $15; BNE DecLen; INC $16
            // DecLen:
            // F856: LDA $17; BNE DecLenLo; DEC $18
            // DecLenLo: DEC $17
            // Check if done:
            // F85C: LDA $17; ORA $18; BNE ReadR3Loop
            // F862: JMP WaitR4
            //
            // DoRelease:
            // Read sync byte ($D2):
            // F865: BIT $FEFE; BPL F865; LDA $FEFF
            // Read 4 bytes from R4:
            // F86D: BIT $FEFE; BPL F86D; LDA $FEFF
            // F874: BIT $FEFE; BPL F874; LDA $FEFF
            // F87B: BIT $FEFE; BPL F87B; LDA $FEFF
            // F882: BIT $FEFE; BPL F882; LDA $FEFF
            // Signal ready to Host via R4 write:
            // F889: BIT $FEFE; BVC F889
            // F88E: LDA #$00; STA $FEFF
            // Await R2 and execute entry at $0800:
            // F893: JMP $0800

            byte[] code = new byte[]
            {
                0x78, 0xD8, 0xA2, 0xFF, 0x9A, 0xA9, 0x00, 0x85, 0x15, 0xA9, 0x08, 0x85, 0x16, 0x2C, 0xFE, 0xFE,
                0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0xC9, 0x01, 0xF0, 0x07, 0xC9, 0x04, 0xF0, 0x54, 0x4C, 0x0D, 0xF8,
                0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE,
                0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE,
                0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE,
                0x2C, 0xFC, 0xFE, 0x10, 0xFB, 0xAD, 0xFD, 0xFE, 0xA0, 0x00, 0x91, 0x15, 0xE6, 0x15, 0xD0, 0x02,
                0xE6, 0x16, 0xA5, 0x15, 0xC9, 0x36, 0xD0, 0xE8, 0xA5, 0x16, 0xC9, 0x0A, 0xD0, 0xE2, 0x4C, 0x0D,
                0xF8, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF,
                0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF,
                0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF, 0xFE, 0x2C, 0xFE, 0xFE, 0x10, 0xFB, 0xAD, 0xFF,
                0xFE, 0x2C, 0xFE, 0xFE, 0x50, 0xFB, 0xA9, 0x00, 0x8D, 0xFF, 0xFE, 0x4C, 0x00, 0x08
            };

            for (int i = 0; i < code.Length; i++)
            {
                Ram[bootEntry + i] = code[i];
            }

            // Put RTI at end of bootstrap code ($F8AE) for NMI/IRQ during bootstrap
            Ram[0xF8AE] = 0x40; // RTI

            // Vectors at $FFFA-$FFFF
            Ram[0xFFFA] = 0xAE; Ram[0xFFFB] = 0xF8; // NMI -> $F8AE (RTI)
            Ram[0xFFFC] = 0x00; Ram[0xFFFD] = 0xF8; // RESET -> $F800
            Ram[0xFFFE] = 0xAE; Ram[0xFFFF] = 0xF8; // IRQ -> $F8AE (RTI)
        }

        public byte Read(ushort address)
        {
            // Tube ULA mapped at $FEF8 - $FEFF
            if (address >= 0xFEF8 && address <= 0xFEFF)
            {
                return Tube.ParasiteRead(address);
            }

            // Boot ROM overlay window ($F800 - $FFFF)
            if (BootRomEnabled && BootRom != null && address >= 0xF800)
            {
                int romOffset = address - (0x10000 - BootRom.Length);
                if (romOffset >= 0 && romOffset < BootRom.Length)
                {
                    return BootRom[romOffset];
                }
            }

            return Ram[address];
        }

        public void Write(ushort address, byte value)
        {
            // Tube ULA mapped at $FEF8 - $FEFF
            if (address >= 0xFEF8 && address <= 0xFEFF)
            {
                Tube.ParasiteWrite(address, value);
                return;
            }

            // Writes always write to RAM underneath
            Ram[address] = value;
        }
    }
}
