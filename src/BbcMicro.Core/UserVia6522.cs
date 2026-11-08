using System;

namespace BbcMicro.Core
{
    public class UserVia6522
    {
        public byte Orb;
        public byte Ora;
        public byte Ddrb;
        public byte Ddra;
        public ushort T1Counter;
        public ushort T1Latch;
        public ushort T2Counter;
        public ushort T2Latch;
        public byte Sr;
        public byte Acr;
        public byte Pcr;
        public byte Ifr;
        public byte Ier;

        public bool IrqActive => (Ifr & 0x7F & Ier) != 0;

        public void Reset()
        {
            Orb = 0xFF;
            Ora = 0xFF;
            Ddrb = 0;
            Ddra = 0;
            T1Counter = 0xFFFF;
            T1Latch = 0xFFFF;
            T2Counter = 0xFFFF;
            T2Latch = 0;
            Sr = 0;
            Acr = 0;
            Pcr = 0;
            Ifr = 0;
            Ier = 0;
        }

        public void Tick(int cycles)
        {
            if (T1Counter <= cycles)
            {
                Ifr |= 0x40;
                T1Counter = (Acr & 0x40) != 0 ? T1Latch : (ushort)0xFFFF;
            }
            else
            {
                T1Counter -= (ushort)cycles;
            }

            if ((Ifr & 0x7F & Ier) != 0)
                Ifr |= 0x80;
            else
                Ifr &= 0x7F;
        }

        public byte Read(int reg)
        {
            return (reg & 0x0F) switch
            {
                0 => Orb,
                1 or 15 => Ora,
                2 => Ddrb,
                3 => Ddra,
                4 => (byte)(T1Counter & 0xFF),
                5 => (byte)(T1Counter >> 8),
                6 => (byte)(T1Latch & 0xFF),
                7 => (byte)(T1Latch >> 8),
                8 => (byte)(T2Counter & 0xFF),
                9 => (byte)(T2Counter >> 8),
                10 => Sr,
                11 => Acr,
                12 => Pcr,
                13 => (byte)((Ifr & 0x7F) | (((Ifr & 0x7F & Ier) != 0) ? 0x80 : 0x00)),
                14 => (byte)(Ier | 0x80),
                _ => 0xFF
            };
        }

        public void Write(int reg, byte val)
        {
            switch (reg & 0x0F)
            {
                case 0: Orb = val; break;
                case 1 or 15: Ora = val; break;
                case 2: Ddrb = val; break;
                case 3: Ddra = val; break;
                case 4 or 6: T1Latch = (ushort)((T1Latch & 0xFF00) | val); break;
                case 5:
                    T1Latch = (ushort)((val << 8) | (T1Latch & 0x00FF));
                    T1Counter = T1Latch;
                    Ifr &= unchecked((byte)~0x40);
                    break;
                case 7: T1Latch = (ushort)((val << 8) | (T1Latch & 0x00FF)); break;
                case 8: T2Latch = (ushort)((T2Latch & 0xFF00) | val); break;
                case 9:
                    T2Latch = (ushort)((val << 8) | (T2Latch & 0x00FF));
                    T2Counter = T2Latch;
                    Ifr &= unchecked((byte)~0x20);
                    break;
                case 10: Sr = val; break;
                case 11: Acr = val; break;
                case 12: Pcr = val; break;
                case 13: Ifr &= (byte)~(val & 0x7F); break;
                case 14:
                    if ((val & 0x80) != 0)
                        Ier |= (byte)(val & 0x7F);
                    else
                        Ier &= (byte)~(val & 0x7F);
                    break;
            }
        }
    }
}
