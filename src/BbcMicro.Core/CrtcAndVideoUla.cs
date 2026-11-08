using System;

namespace BbcMicro.Core
{
    public class Crtc6845
    {
        public byte AddressRegister;
        public readonly byte[] Registers = new byte[32];

        public int HorizontalTotal => Registers[0];
        public int HorizontalDisplayed => Registers[1];
        public int HorizontalSyncPos => Registers[2];
        public int SyncWidth => Registers[3];
        public int VerticalTotal => Registers[4];
        public int VerticalTotalAdjust => Registers[5];
        public int VerticalDisplayed => Registers[6];
        public int VerticalSyncPos => Registers[7];
        public int InterlaceMode => Registers[8];
        public int MaxScanLineAddress => Registers[9];
        public int CursorStart => Registers[10];
        public int CursorEnd => Registers[11];
        public ushort ScreenStartAddress => (ushort)(((Registers[12] & 0x3F) << 8) | Registers[13]);
        public ushort CursorAddress => (ushort)(((Registers[14] & 0x3F) << 8) | Registers[15]);

        public void Reset()
        {
            AddressRegister = 0;
            Array.Clear(Registers, 0, Registers.Length);
            // Default 6845 Mode 7 settings:
            Registers[0] = 63;  // Horiz total
            Registers[1] = 40;  // Horiz displayed (40 characters)
            Registers[2] = 49;  // Horiz sync pos
            Registers[3] = 0x24;// Sync widths
            Registers[4] = 30;  // Vert total
            Registers[5] = 2;   // Vert adjust
            Registers[6] = 25;  // Vert displayed (25 rows)
            Registers[7] = 28;  // Vert sync pos
            Registers[8] = 0;
            Registers[9] = 18;  // Scanlines per character row
            Registers[12] = 0x28; // Start address $7C00 (address >> 3)
            Registers[13] = 0x00;
        }

        public byte Read(int offset)
        {
            if ((offset & 1) == 1)
            {
                if (AddressRegister < Registers.Length)
                    return Registers[AddressRegister];
            }
            return AddressRegister;
        }

        public void Write(int offset, byte val)
        {
            if ((offset & 1) == 0)
            {
                AddressRegister = (byte)(val & 0x1F);
            }
            else
            {
                if (AddressRegister < Registers.Length)
                    Registers[AddressRegister] = val;
            }
        }
    }

    public class VideoUla
    {
        public byte Control;
        public readonly byte[] Palette = new byte[16];

        public void Reset()
        {
            Control = 0x9C; // Mode 7 Teletext default
            Array.Clear(Palette, 0, Palette.Length);
        }

        public byte Read(int offset)
        {
            return Control;
        }

        public void Write(int offset, byte val)
        {
            if ((offset & 1) == 0)
            {
                Control = val;
            }
            else
            {
                int index = (val >> 4) & 0x0F;
                Palette[index] = (byte)(val & 0x0F);
            }
        }
    }
}
