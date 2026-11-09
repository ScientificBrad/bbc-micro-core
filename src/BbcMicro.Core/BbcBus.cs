using System;
using System.IO;
using System.Text;

namespace BbcMicro.Core
{
    public class BbcBus : IMemoryBus
    {
        public readonly byte[] Ram = new byte[32768];
        public readonly byte[] MosRom = new byte[16384];
        public readonly byte[][] SidewaysRoms = new byte[16][];

        public int SelectedSlot = 15; // Defaults to Slot 15 (BASIC)

        public readonly SystemVia6522 SystemVia;
        public readonly Sn76489 Sound = new();
        public readonly UserVia6522 UserVia;
        public readonly Crtc6845 Crtc = new();
        public readonly VideoUla Ula = new();
        public readonly KeyboardMatrix Keyboard = new();
        public readonly Intel8271 Fdc;
        public TubeUla? Tube { get; set; }

        public readonly StringBuilder VduOutput = new();
        public Action<char>? OnCharacterWritten;

        public bool IrqPending => SystemVia.IrqActive || UserVia.IrqActive || (Tube != null && Tube.HostIrq);
        private bool _nmiPending;
        public bool NmiPending
        {
            get => _nmiPending || (Tube != null && Tube.HostNmi);
            set => _nmiPending = value;
        }

        public void ClearNmi()
        {
            _nmiPending = false;
            Tube?.ClearHostNmi();
        }

        public BbcBus(byte[]? mosRom = null, byte[]? basicRom = null, byte[]? dfsRom = null)
        {
            Fdc = new Intel8271(this);
            SystemVia = new SystemVia6522(Keyboard);
            SystemVia.SoundWrite = Sound.Write;
            UserVia = new UserVia6522();

            for (int i = 0; i < 16; i++)
            {
                SidewaysRoms[i] = new byte[16384];
                Array.Fill(SidewaysRoms[i], (byte)0xFF);
            }

            if (mosRom != null)
            {
                Buffer.BlockCopy(mosRom, 0, MosRom, 0, Math.Min(mosRom.Length, MosRom.Length));
            }
            if (basicRom != null)
            {
                Buffer.BlockCopy(basicRom, 0, SidewaysRoms[15], 0, Math.Min(basicRom.Length, 16384));
            }
            if (dfsRom != null)
            {
                // DFS is 8KB, mapped at $8000-$9FFF
                Buffer.BlockCopy(dfsRom, 0, SidewaysRoms[14], 0, Math.Min(dfsRom.Length, 16384));
                // Duplicate into upper 8KB if 8KB image
                if (dfsRom.Length == 8192)
                    Buffer.BlockCopy(dfsRom, 0, SidewaysRoms[14], 8192, 8192);
            }

            Reset();
        }

        public void Reset()
        {
            Array.Clear(Ram, 0, Ram.Length);
            SelectedSlot = 15;
            SystemVia.Reset();
            Sound.Reset();
            UserVia.Reset();
            Crtc.Reset();
            Ula.Reset();
            Keyboard.Reset();
            Fdc.Reset();
            VduOutput.Clear();
            NmiPending = false;
        }

        public void Tick(int cycles)
        {
            SystemVia.Tick(cycles);
            Sound.Tick(cycles);
            UserVia.Tick(cycles);
            Keyboard.Tick();
            Fdc.Tick(cycles);
        }

        public byte Read(ushort address)
        {
            // 0x0000 - 0x7FFF: RAM (32KB)
            if (address < 0x8000)
            {
                return Ram[address];
            }

            // 0x8000 - 0xBFFF: Sideways ROM (16KB)
            if (address < 0xC000)
            {
                return SidewaysRoms[SelectedSlot][address - 0x8000];
            }

            // 0xC000 - 0xFFFF: MOS 1.20 / I/O
            // 0xFC00 - 0xFEFF: I/O area
            if (address >= 0xFC00 && address < 0xFF00)
            {
                if (address < 0xFD00) // FRED ($FC00-$FCFF)
                {
                    return 0xFF;
                }
                if (address < 0xFE00) // JIM ($FD00-$FDFF)
                {
                    return 0xFF;
                }

                // SHEILA ($FE00-$FEFF)
                return ReadSheila((byte)(address & 0xFF));
            }

            // Operating System ROM ($C000-$FC00 and $FF00-$FFFF)
            return MosRom[address - 0xC000];
        }

        public void Write(ushort address, byte value)
        {
            // 0x0000 - 0x7FFF: RAM (32KB)
            if (address < 0x8000)
            {
                Ram[address] = value;
                return;
            }

            // 0x8000 - 0xBFFF: Sideways ROM (Write Protected on Model B)
            if (address < 0xC000)
            {
                return;
            }

            // 0xFC00 - 0xFEFF: Memory mapped I/O
            if (address >= 0xFC00 && address < 0xFF00)
            {
                if (address >= 0xFE00)
                {
                    WriteSheila((byte)(address & 0xFF), value);
                }
                return;
            }

            // 0xC000 - 0xFFFF: ROM (writes ignored)
        }

        private byte ReadSheila(byte offset)
        {
            // 6845 CRTC ($FE00-$FE07)
            if (offset < 0x08)
            {
                return Crtc.Read(offset);
            }
            // 6850 ACIA ($FE08-$FE0F)
            if (offset < 0x10)
            {
                // Bit 1: Tx ready (always ready for emulator), Bit 0: Rx ready
                if ((offset & 1) == 0) return 0x02; // Status: Tx empty
                return 0x00; // Rx data
            }
            // Serial ULA ($FE10-$FE1F)
            if (offset < 0x20)
            {
                return 0x00;
            }
            // Video ULA ($FE20-$FE2F)
            if (offset < 0x30)
            {
                return Ula.Read(offset);
            }
            // ROM Select Latch ($FE30-$FE3F)
            if (offset < 0x40)
            {
                return (byte)SelectedSlot;
            }
            // System VIA 6522 ($FE40-$FE5F)
            if (offset < 0x60)
            {
                return SystemVia.Read(offset);
            }
            // User VIA 6522 ($FE60-$FE7F)
            if (offset < 0x80)
            {
                return UserVia.Read(offset);
            }
            // Floppy Controller 8271 ($FE80-$FE9F)
            if (offset < 0xA0)
            {
                return Fdc.Read((byte)(offset - 0x80));
            }
            // Econet ($FEA0-$FEBF)
            if (offset < 0xC0)
            {
                return 0x00;
            }
            // ADC 7002 ($FEC0-$FEDF)
            if (offset < 0xE0)
            {
                return 0x40; // Conversion complete flag
            }
            // Tube ULA ($FEE0-$FEFF)
            if (offset >= 0xE0)
            {
                return Tube != null ? Tube.HostRead((ushort)(0xFE00 + offset)) : (byte)0x00;
            }
            return 0x00;
        }

        private void WriteSheila(byte offset, byte value)
        {
            // 6845 CRTC ($FE00-$FE07)
            if (offset < 0x08)
            {
                Crtc.Write(offset, value);
                return;
            }
            // 6850 ACIA ($FE08-$FE0F)
            if (offset < 0x10)
            {
                return;
            }
            // Serial ULA ($FE10-$FE1F)
            if (offset < 0x20)
            {
                return;
            }
            // Video ULA ($FE20-$FE2F)
            if (offset < 0x30)
            {
                Ula.Write(offset, value);
                return;
            }
            // ROM Select Latch ($FE30-$FE3F)
            if (offset < 0x40)
            {
                SelectedSlot = value & 0x0F;
                return;
            }
            // System VIA 6522 ($FE40-$FE5F)
            if (offset < 0x60)
            {
                SystemVia.Write(offset, value);
                return;
            }
            // User VIA 6522 ($FE60-$FE7F)
            if (offset < 0x80)
            {
                UserVia.Write(offset, value);
                return;
            }
            // Floppy Controller 8271 ($FE80-$FE9F)
            if (offset < 0xA0)
            {
                Fdc.Write((byte)(offset - 0x80), value);
                return;
            }
            // Tube ULA ($FEE0-$FEFF)
            if (offset >= 0xE0)
            {
                Tube?.HostWrite((ushort)(0xFE00 + offset), value);
                return;
            }
        }
    }
}
