using System;

namespace BbcMicro.Core
{
    public class SystemVia6522
    {
        public byte Orb;   // Reg 0
        public byte Ora;   // Reg 1
        public byte Ddrb;  // Reg 2
        public byte Ddra;  // Reg 3
        
        public ushort T1Counter;
        public ushort T1Latch;
        public ushort T2Counter;
        public ushort T2Latch;

        public byte Sr;    // Reg 10
        public byte Acr;   // Reg 11
        public byte Pcr;   // Reg 12
        public byte Ifr;   // Reg 13
        public byte Ier;   // Reg 14

        private readonly KeyboardMatrix _keyboard;
        private byte _addressableLatch = 0xFF;
        public Action<byte>? SoundWrite;
        public byte AddressableLatch => _addressableLatch;
        private int _vsyncCycleCounter;

        public bool IrqActive => (Ifr & 0x7F & Ier) != 0;

        public SystemVia6522(KeyboardMatrix keyboard)
        {
            _keyboard = keyboard;
            Reset();
        }

        public void Reset()
        {
            Orb = 0;
            Ora = 0;
            Ddrb = 0;
            Ddra = 0;
            T1Counter = 0xFFFF;
            T1Latch = 0x270E;
            T2Counter = 0xFFFF;
            T2Latch = 0;
            Sr = 0;
            Acr = 0;
            Pcr = 0;
            Ifr = 0;
            Ier = 0;
            _vsyncCycleCounter = 0;
            _addressableLatch = 0xFF;
        }

        public void Tick(int cycles)
        {
            // Timer 1 countdown
            if (T1Counter <= cycles)
            {
                // Underflow
                Ifr |= 0x40; // Bit 6: Timer 1
                if ((Acr & 0x40) != 0) // Continuous / free-run mode
                {
                    int reload = T1Latch - (cycles - T1Counter);
                    T1Counter = (ushort)(reload > 0 ? reload : T1Latch);
                }
                else
                {
                    T1Counter = 0xFFFF;
                }
            }
            else
            {
                T1Counter -= (ushort)cycles;
            }

            // VSync (50Hz = every 40,000 cycles at 2MHz)
            _vsyncCycleCounter += cycles;
            if (_vsyncCycleCounter >= 40000)
            {
                _vsyncCycleCounter -= 40000;
                Ifr |= 0x02; // Bit 1: CA1 (Vertical Sync)
            }

            // Keyboard auto-scan (IC32 addressable latch output 3 = 1):
            // Hardware counter scans columns continuously; if any key is held down, assert CA2
            if ((_addressableLatch & 0x08) != 0 && _keyboard.HasAnyKeyDown())
            {
                Ifr |= 0x01; // Bit 0: CA2 (Keyboard interrupt)
            }

            // Update IFR bit 7 (active IRQ)
            if ((Ifr & 0x7F & Ier) != 0)
                Ifr |= 0x80;
            else
                Ifr &= 0x7F;
        }

        public byte Read(int reg)
        {
            switch (reg & 0x0F)
            {
                case 0: // Port B
                    return (byte)((Orb & Ddrb) | (0xF0 & ~Ddrb));

                case 1: // Port A with handshake
                    Ifr &= unchecked((byte)~0x03);
                    return ReadPortA();

                case 2: // DDRB
                    return Ddrb;

                case 3: // DDRA
                    return Ddra;

                case 4: // T1C-L
                    Ifr &= unchecked((byte)~0x40);
                    return (byte)(T1Counter & 0xFF);

                case 5: // T1C-H
                    return (byte)(T1Counter >> 8);

                case 6: // T1L-L
                    return (byte)(T1Latch & 0xFF);

                case 7: // T1L-H
                    return (byte)(T1Latch >> 8);

                case 8: // T2C-L
                    Ifr &= unchecked((byte)~0x20);
                    return (byte)(T2Counter & 0xFF);

                case 9: // T2C-H
                    return (byte)(T2Counter >> 8);

                case 10: // SR
                    return Sr;

                case 11: // ACR
                    return Acr;

                case 12: // PCR
                    return Pcr;

                case 13: // IFR
                    byte ifrRead = (byte)(Ifr & 0x7F);
                    if ((ifrRead & Ier) != 0)
                        ifrRead |= 0x80;
                    return ifrRead;

                case 14: // IER
                    return (byte)(Ier | 0x80);

                case 15: // Port A without handshake
                    return ReadPortA();

                default:
                    return 0xFF;
            }
        }

        private byte ReadPortA()
        {
            byte keyVal = _keyboard.InterrogateKey((byte)(Ora & 0x7F));
            return (byte)((Ora & Ddra) | (keyVal & ~Ddra));
        }

        public void Write(int reg, byte val)
        {
            switch (reg & 0x0F)
            {
                case 0: // Port B
                    Orb = val;
                    // IC32 74LS259 latch: PB0-2 select output, PB3 sets its value.
                    // Output 0 is the active-low SN76489 write-enable signal.
                    int index = val & 7;
                    int mask = 1 << index;
                    bool wasHigh = (_addressableLatch & mask) != 0;
                    bool nowHigh = (val & 8) != 0;
                    _addressableLatch = nowHigh
                        ? (byte)(_addressableLatch | mask)
                        : (byte)(_addressableLatch & ~mask);
                    if (index == 0 && wasHigh && !nowHigh)
                        SoundWrite?.Invoke(Ora);
                    break;

                case 1: // Port A with handshake
                    Ifr &= unchecked((byte)~0x03);
                    Ora = val;
                    UpdateKeyboardCol(val);
                    break;

                case 2: // DDRB
                    Ddrb = val;
                    break;

                case 3: // DDRA
                    Ddra = val;
                    break;

                case 4: // T1L-L
                    T1Latch = (ushort)((T1Latch & 0xFF00) | val);
                    break;

                case 5: // T1C-H (starts timer)
                    T1Latch = (ushort)((val << 8) | (T1Latch & 0x00FF));
                    T1Counter = T1Latch;
                    Ifr &= unchecked((byte)~0x40);
                    break;

                case 6: // T1L-L
                    T1Latch = (ushort)((T1Latch & 0xFF00) | val);
                    break;

                case 7: // T1L-H
                    T1Latch = (ushort)((val << 8) | (T1Latch & 0x00FF));
                    break;

                case 8: // T2L-L
                    T2Latch = (ushort)((T2Latch & 0xFF00) | val);
                    break;

                case 9: // T2C-H
                    T2Latch = (ushort)((val << 8) | (T2Latch & 0x00FF));
                    T2Counter = T2Latch;
                    Ifr &= unchecked((byte)~0x20);
                    break;

                case 10: // SR
                    Sr = val;
                    break;

                case 11: // ACR
                    Acr = val;
                    break;

                case 12: // PCR
                    Pcr = val;
                    break;

                case 13: // IFR
                    Ifr &= (byte)~(val & 0x7F);
                    break;

                case 14: // IER
                    if ((val & 0x80) != 0)
                        Ier |= (byte)(val & 0x7F);
                    else
                        Ier &= (byte)~(val & 0x7F);
                    break;

                case 15: // Port A without handshake
                    Ora = val;
                    UpdateKeyboardCol(val);
                    break;
            }
        }

        private void UpdateKeyboardCol(byte val)
        {
            if (_keyboard.HasKeyInColumn((byte)(val & 0x0F)))
            {
                Ifr |= 0x01; // CA2 interrupt active
            }
            else
            {
                Ifr &= unchecked((byte)~0x01);
            }
        }
    }
}
