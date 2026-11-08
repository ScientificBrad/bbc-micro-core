using System;

namespace BbcMicro.Core
{
    public interface IMemoryBus
    {
        byte Read(ushort address);
        void Write(ushort address, byte value);
        bool IrqPending { get; }
        bool NmiPending { get; }
        void ClearNmi();
        void Tick(int cycles);
    }

    public class Cpu6502
    {
        public byte A, X, Y, SP;
        public ushort PC;
        public bool C, Z, I, D, B, V, N;

        private readonly IMemoryBus _bus;
        public long TotalCycles { get; private set; }
        public long InstructionsExecuted { get; private set; }

        public byte Status
        {
            get
            {
                byte res = 0x20; // Bit 5 is always 1 in P register
                if (C) res |= 0x01;
                if (Z) res |= 0x02;
                if (I) res |= 0x04;
                if (D) res |= 0x08;
                if (B) res |= 0x10;
                if (V) res |= 0x40;
                if (N) res |= 0x80;
                return res;
            }
            set
            {
                C = (value & 0x01) != 0;
                Z = (value & 0x02) != 0;
                I = (value & 0x04) != 0;
                D = (value & 0x08) != 0;
                B = (value & 0x10) != 0;
                V = (value & 0x40) != 0;
                N = (value & 0x80) != 0;
            }
        }

        public Cpu6502(IMemoryBus bus)
        {
            _bus = bus;
            Reset();
        }

        public void Reset()
        {
            SP = 0xFF;
            Status = 0x24; // I set, bit 5 set
            ushort lo = _bus.Read(0xFFFC);
            ushort hi = _bus.Read(0xFFFD);
            PC = (ushort)((hi << 8) | lo);
            TotalCycles = 7;
        }

        private void Push(byte val)
        {
            _bus.Write((ushort)(0x0100 | SP), val);
            SP--;
        }

        private byte Pop()
        {
            SP++;
            return _bus.Read((ushort)(0x0100 | SP));
        }

        private void PushWord(ushort val)
        {
            Push((byte)(val >> 8));
            Push((byte)(val & 0xFF));
        }

        private ushort PopWord()
        {
            byte lo = Pop();
            byte hi = Pop();
            return (ushort)((hi << 8) | lo);
        }

        private void SetZN(byte val)
        {
            Z = (val == 0);
            N = (val & 0x80) != 0;
        }

        private byte ReadOpcode()
        {
            return _bus.Read(PC++);
        }

        private byte ReadByte()
        {
            return _bus.Read(PC++);
        }

        private ushort ReadWord()
        {
            byte lo = _bus.Read(PC++);
            byte hi = _bus.Read(PC++);
            return (ushort)((hi << 8) | lo);
        }

        private ushort ReadWordBug(ushort addr)
        {
            // 6502 indirect JMP page boundary wrap bug
            byte lo = _bus.Read(addr);
            ushort addrHi = (ushort)((addr & 0xFF00) | ((addr + 1) & 0x00FF));
            byte hi = _bus.Read(addrHi);
            return (ushort)((hi << 8) | lo);
        }

        // Addressing modes
        private ushort AddrZeroPage() => ReadByte();
        private ushort AddrZeroPageX() => (byte)(ReadByte() + X);
        private ushort AddrZeroPageY() => (byte)(ReadByte() + Y);
        private ushort AddrAbsolute() => ReadWord();
        private ushort AddrAbsoluteX(out bool pageCrossed)
        {
            ushort baseAddr = ReadWord();
            ushort addr = (ushort)(baseAddr + X);
            pageCrossed = (baseAddr & 0xFF00) != (addr & 0xFF00);
            return addr;
        }
        private ushort AddrAbsoluteY(out bool pageCrossed)
        {
            ushort baseAddr = ReadWord();
            ushort addr = (ushort)(baseAddr + Y);
            pageCrossed = (baseAddr & 0xFF00) != (addr & 0xFF00);
            return addr;
        }
        private ushort AddrIndirectX()
        {
            byte ptr = (byte)(ReadByte() + X);
            byte lo = _bus.Read(ptr);
            byte hi = _bus.Read((byte)(ptr + 1));
            return (ushort)((hi << 8) | lo);
        }
        private ushort AddrIndirectY(out bool pageCrossed)
        {
            byte ptr = ReadByte();
            byte lo = _bus.Read(ptr);
            byte hi = _bus.Read((byte)(ptr + 1));
            ushort baseAddr = (ushort)((hi << 8) | lo);
            ushort addr = (ushort)(baseAddr + Y);
            pageCrossed = (baseAddr & 0xFF00) != (addr & 0xFF00);
            return addr;
        }

        private void Branch(bool condition, ref int cycles)
        {
            sbyte offset = (sbyte)ReadByte();
            if (condition)
            {
                cycles++;
                ushort newPc = (ushort)(PC + offset);
                if ((PC & 0xFF00) != (newPc & 0xFF00))
                    cycles++;
                PC = newPc;
            }
        }

        private void Compare(byte reg, byte val)
        {
            int diff = reg - val;
            C = reg >= val;
            SetZN((byte)diff);
        }

        private void Adc(byte val)
        {
            if (D)
            {
                // BCD mode
                int cIn = C ? 1 : 0;
                int al = (A & 0x0F) + (val & 0x0F) + cIn;
                if (al > 9) al += 6;
                int ah = (A >> 4) + (val >> 4) + (al > 15 ? 1 : 0);
                
                // Overflow flag is calculated using binary addition
                int sumBin = A + val + cIn;
                V = (~(A ^ val) & (A ^ (byte)sumBin) & 0x80) != 0;
                
                if (ah > 9) ah += 6;
                C = ah > 15;
                A = (byte)((ah << 4) | (al & 0x0F));
                SetZN(A);
            }
            else
            {
                int sum = A + val + (C ? 1 : 0);
                V = (~(A ^ val) & (A ^ sum) & 0x80) != 0;
                C = sum > 0xFF;
                A = (byte)sum;
                SetZN(A);
            }
        }

        private void Sbc(byte val)
        {
            if (D)
            {
                // BCD mode
                int cIn = C ? 0 : 1;
                int diff = A - val - cIn;
                int al = (A & 0x0F) - (val & 0x0F) - cIn;
                if (al < 0) al -= 6;
                int ah = (A >> 4) - (val >> 4) - (al < 0 ? 1 : 0);
                if (ah < 0) ah -= 6;

                // Overflow flag is calculated using binary subtraction
                int binDiff = A - val - cIn;
                V = ((A ^ val) & (A ^ (byte)binDiff) & 0x80) != 0;
                C = diff >= 0;
                A = (byte)((ah << 4) | (al & 0x0F));
                SetZN(A);
            }
            else
            {
                int diff = A - val - (C ? 0 : 1);
                V = ((A ^ val) & (A ^ diff) & 0x80) != 0;
                C = diff >= 0;
                A = (byte)diff;
                SetZN(A);
            }
        }

        public int Step()
        {
            // Interrupt check
            if (_bus.NmiPending)
            {
                _bus.ClearNmi();
                PushWord(PC);
                Push((byte)((Status & ~0x10) | 0x20));
                I = true;
                PC = (ushort)(_bus.Read(0xFFFA) | (_bus.Read(0xFFFB) << 8));
                _bus.Tick(7);
                TotalCycles += 7;
                return 7;
            }
            if (_bus.IrqPending && !I)
            {
                PushWord(PC);
                Push((byte)((Status & ~0x10) | 0x20));
                I = true;
                PC = (ushort)(_bus.Read(0xFFFE) | (_bus.Read(0xFFFF) << 8));
                _bus.Tick(7);
                TotalCycles += 7;
                return 7;
            }

            byte op = ReadOpcode();
            int cycles = 2;
            bool cross;
            ushort addr;
            byte val;

            switch (op)
            {
                // LDA
                case 0xA9: A = ReadByte(); SetZN(A); cycles = 2; break;
                case 0xA5: A = _bus.Read(AddrZeroPage()); SetZN(A); cycles = 3; break;
                case 0xB5: A = _bus.Read(AddrZeroPageX()); SetZN(A); cycles = 4; break;
                case 0xAD: A = _bus.Read(AddrAbsolute()); SetZN(A); cycles = 4; break;
                case 0xBD: A = _bus.Read(AddrAbsoluteX(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0xB9: A = _bus.Read(AddrAbsoluteY(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0xA1: A = _bus.Read(AddrIndirectX()); SetZN(A); cycles = 6; break;
                case 0xB1: A = _bus.Read(AddrIndirectY(out cross)); SetZN(A); cycles = cross ? 6 : 5; break;

                // LDX
                case 0xA2: X = ReadByte(); SetZN(X); cycles = 2; break;
                case 0xA6: X = _bus.Read(AddrZeroPage()); SetZN(X); cycles = 3; break;
                case 0xB6: X = _bus.Read(AddrZeroPageY()); SetZN(X); cycles = 4; break;
                case 0xAE: X = _bus.Read(AddrAbsolute()); SetZN(X); cycles = 4; break;
                case 0xBE: X = _bus.Read(AddrAbsoluteY(out cross)); SetZN(X); cycles = cross ? 5 : 4; break;

                // LDY
                case 0xA0: Y = ReadByte(); SetZN(Y); cycles = 2; break;
                case 0xA4: Y = _bus.Read(AddrZeroPage()); SetZN(Y); cycles = 3; break;
                case 0xB4: Y = _bus.Read(AddrZeroPageX()); SetZN(Y); cycles = 4; break;
                case 0xAC: Y = _bus.Read(AddrAbsolute()); SetZN(Y); cycles = 4; break;
                case 0xBC: Y = _bus.Read(AddrAbsoluteX(out cross)); SetZN(Y); cycles = cross ? 5 : 4; break;

                // STA
                case 0x85: _bus.Write(AddrZeroPage(), A); cycles = 3; break;
                case 0x95: _bus.Write(AddrZeroPageX(), A); cycles = 4; break;
                case 0x8D: _bus.Write(AddrAbsolute(), A); cycles = 4; break;
                case 0x9D: AddrAbsoluteX(out _); _bus.Write((ushort)(AddrAbsoluteXBase() + X), A); cycles = 5; break;
                case 0x99: AddrAbsoluteY(out _); _bus.Write((ushort)(AddrAbsoluteYBase() + Y), A); cycles = 5; break;
                case 0x81: _bus.Write(AddrIndirectX(), A); cycles = 6; break;
                case 0x91: AddrIndirectY(out _); _bus.Write(AddrIndirectYTarget(), A); cycles = 6; break;

                // STX
                case 0x86: _bus.Write(AddrZeroPage(), X); cycles = 3; break;
                case 0x96: _bus.Write(AddrZeroPageY(), X); cycles = 4; break;
                case 0x8E: _bus.Write(AddrAbsolute(), X); cycles = 4; break;

                // STY
                case 0x84: _bus.Write(AddrZeroPage(), Y); cycles = 3; break;
                case 0x94: _bus.Write(AddrZeroPageX(), Y); cycles = 4; break;
                case 0x8C: _bus.Write(AddrAbsolute(), Y); cycles = 4; break;

                // ADC
                case 0x69: Adc(ReadByte()); cycles = 2; break;
                case 0x65: Adc(_bus.Read(AddrZeroPage())); cycles = 3; break;
                case 0x75: Adc(_bus.Read(AddrZeroPageX())); cycles = 4; break;
                case 0x6D: Adc(_bus.Read(AddrAbsolute())); cycles = 4; break;
                case 0x7D: Adc(_bus.Read(AddrAbsoluteX(out cross))); cycles = cross ? 5 : 4; break;
                case 0x79: Adc(_bus.Read(AddrAbsoluteY(out cross))); cycles = cross ? 5 : 4; break;
                case 0x61: Adc(_bus.Read(AddrIndirectX())); cycles = 6; break;
                case 0x71: Adc(_bus.Read(AddrIndirectY(out cross))); cycles = cross ? 6 : 5; break;

                // SBC
                case 0xE9: Sbc(ReadByte()); cycles = 2; break;
                case 0xE5: Sbc(_bus.Read(AddrZeroPage())); cycles = 3; break;
                case 0xF5: Sbc(_bus.Read(AddrZeroPageX())); cycles = 4; break;
                case 0xED: Sbc(_bus.Read(AddrAbsolute())); cycles = 4; break;
                case 0xFD: Sbc(_bus.Read(AddrAbsoluteX(out cross))); cycles = cross ? 5 : 4; break;
                case 0xF9: Sbc(_bus.Read(AddrAbsoluteY(out cross))); cycles = cross ? 5 : 4; break;
                case 0xE1: Sbc(_bus.Read(AddrIndirectX())); cycles = 6; break;
                case 0xF1: Sbc(_bus.Read(AddrIndirectY(out cross))); cycles = cross ? 6 : 5; break;

                // CMP
                case 0xC9: Compare(A, ReadByte()); cycles = 2; break;
                case 0xC5: Compare(A, _bus.Read(AddrZeroPage())); cycles = 3; break;
                case 0xD5: Compare(A, _bus.Read(AddrZeroPageX())); cycles = 4; break;
                case 0xCD: Compare(A, _bus.Read(AddrAbsolute())); cycles = 4; break;
                case 0xDD: Compare(A, _bus.Read(AddrAbsoluteX(out cross))); cycles = cross ? 5 : 4; break;
                case 0xD9: Compare(A, _bus.Read(AddrAbsoluteY(out cross))); cycles = cross ? 5 : 4; break;
                case 0xC1: Compare(A, _bus.Read(AddrIndirectX())); cycles = 6; break;
                case 0xD1: Compare(A, _bus.Read(AddrIndirectY(out cross))); cycles = cross ? 6 : 5; break;

                // CPX
                case 0xE0: Compare(X, ReadByte()); cycles = 2; break;
                case 0xE4: Compare(X, _bus.Read(AddrZeroPage())); cycles = 3; break;
                case 0xEC: Compare(X, _bus.Read(AddrAbsolute())); cycles = 4; break;

                // CPY
                case 0xC0: Compare(Y, ReadByte()); cycles = 2; break;
                case 0xC4: Compare(Y, _bus.Read(AddrZeroPage())); cycles = 3; break;
                case 0xCC: Compare(Y, _bus.Read(AddrAbsolute())); cycles = 4; break;

                // AND
                case 0x29: A &= ReadByte(); SetZN(A); cycles = 2; break;
                case 0x25: A &= _bus.Read(AddrZeroPage()); SetZN(A); cycles = 3; break;
                case 0x35: A &= _bus.Read(AddrZeroPageX()); SetZN(A); cycles = 4; break;
                case 0x2D: A &= _bus.Read(AddrAbsolute()); SetZN(A); cycles = 4; break;
                case 0x3D: A &= _bus.Read(AddrAbsoluteX(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x39: A &= _bus.Read(AddrAbsoluteY(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x21: A &= _bus.Read(AddrIndirectX()); SetZN(A); cycles = 6; break;
                case 0x31: A &= _bus.Read(AddrIndirectY(out cross)); SetZN(A); cycles = cross ? 6 : 5; break;

                // ORA
                case 0x09: A |= ReadByte(); SetZN(A); cycles = 2; break;
                case 0x05: A |= _bus.Read(AddrZeroPage()); SetZN(A); cycles = 3; break;
                case 0x15: A |= _bus.Read(AddrZeroPageX()); SetZN(A); cycles = 4; break;
                case 0x0D: A |= _bus.Read(AddrAbsolute()); SetZN(A); cycles = 4; break;
                case 0x1D: A |= _bus.Read(AddrAbsoluteX(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x19: A |= _bus.Read(AddrAbsoluteY(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x01: A |= _bus.Read(AddrIndirectX()); SetZN(A); cycles = 6; break;
                case 0x11: A |= _bus.Read(AddrIndirectY(out cross)); SetZN(A); cycles = cross ? 6 : 5; break;

                // EOR
                case 0x49: A ^= ReadByte(); SetZN(A); cycles = 2; break;
                case 0x45: A ^= _bus.Read(AddrZeroPage()); SetZN(A); cycles = 3; break;
                case 0x55: A ^= _bus.Read(AddrZeroPageX()); SetZN(A); cycles = 4; break;
                case 0x4D: A ^= _bus.Read(AddrAbsolute()); SetZN(A); cycles = 4; break;
                case 0x5D: A ^= _bus.Read(AddrAbsoluteX(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x59: A ^= _bus.Read(AddrAbsoluteY(out cross)); SetZN(A); cycles = cross ? 5 : 4; break;
                case 0x41: A ^= _bus.Read(AddrIndirectX()); SetZN(A); cycles = 6; break;
                case 0x51: A ^= _bus.Read(AddrIndirectY(out cross)); SetZN(A); cycles = cross ? 6 : 5; break;

                // BIT
                case 0x24:
                    val = _bus.Read(AddrZeroPage());
                    Z = (A & val) == 0;
                    N = (val & 0x80) != 0;
                    V = (val & 0x40) != 0;
                    cycles = 3;
                    break;
                case 0x2C:
                    val = _bus.Read(AddrAbsolute());
                    Z = (A & val) == 0;
                    N = (val & 0x80) != 0;
                    V = (val & 0x40) != 0;
                    cycles = 4;
                    break;

                // INC / DEC memory
                case 0xE6: addr = AddrZeroPage(); val = (byte)(_bus.Read(addr) + 1); _bus.Write(addr, val); SetZN(val); cycles = 5; break;
                case 0xF6: addr = AddrZeroPageX(); val = (byte)(_bus.Read(addr) + 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0xEE: addr = AddrAbsolute(); val = (byte)(_bus.Read(addr) + 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0xFE: AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = (byte)(_bus.Read(addr) + 1); _bus.Write(addr, val); SetZN(val); cycles = 7; break;

                case 0xC6: addr = AddrZeroPage(); val = (byte)(_bus.Read(addr) - 1); _bus.Write(addr, val); SetZN(val); cycles = 5; break;
                case 0xD6: addr = AddrZeroPageX(); val = (byte)(_bus.Read(addr) - 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0xCE: addr = AddrAbsolute(); val = (byte)(_bus.Read(addr) - 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0xDE: AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = (byte)(_bus.Read(addr) - 1); _bus.Write(addr, val); SetZN(val); cycles = 7; break;

                // Register increments / decrements
                case 0xE8: X++; SetZN(X); cycles = 2; break;
                case 0xC8: Y++; SetZN(Y); cycles = 2; break;
                case 0xCA: X--; SetZN(X); cycles = 2; break;
                case 0x88: Y--; SetZN(Y); cycles = 2; break;

                // ASL
                case 0x0A: C = (A & 0x80) != 0; A = (byte)(A << 1); SetZN(A); cycles = 2; break;
                case 0x06: addr = AddrZeroPage(); val = _bus.Read(addr); C = (val & 0x80) != 0; val = (byte)(val << 1); _bus.Write(addr, val); SetZN(val); cycles = 5; break;
                case 0x16: addr = AddrZeroPageX(); val = _bus.Read(addr); C = (val & 0x80) != 0; val = (byte)(val << 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0x0E: addr = AddrAbsolute(); val = _bus.Read(addr); C = (val & 0x80) != 0; val = (byte)(val << 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0x1E: AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = _bus.Read(addr); C = (val & 0x80) != 0; val = (byte)(val << 1); _bus.Write(addr, val); SetZN(val); cycles = 7; break;

                // LSR
                case 0x4A: C = (A & 0x01) != 0; A = (byte)(A >> 1); SetZN(A); cycles = 2; break;
                case 0x46: addr = AddrZeroPage(); val = _bus.Read(addr); C = (val & 0x01) != 0; val = (byte)(val >> 1); _bus.Write(addr, val); SetZN(val); cycles = 5; break;
                case 0x56: addr = AddrZeroPageX(); val = _bus.Read(addr); C = (val & 0x01) != 0; val = (byte)(val >> 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0x4E: addr = AddrAbsolute(); val = _bus.Read(addr); C = (val & 0x01) != 0; val = (byte)(val >> 1); _bus.Write(addr, val); SetZN(val); cycles = 6; break;
                case 0x5E: AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = _bus.Read(addr); C = (val & 0x01) != 0; val = (byte)(val >> 1); _bus.Write(addr, val); SetZN(val); cycles = 7; break;

                // ROL
                case 0x2A: { bool oldC = C; C = (A & 0x80) != 0; A = (byte)((A << 1) | (oldC ? 1 : 0)); SetZN(A); cycles = 2; break; }
                case 0x26: { addr = AddrZeroPage(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x80) != 0; val = (byte)((val << 1) | (oldC ? 1 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 5; break; }
                case 0x36: { addr = AddrZeroPageX(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x80) != 0; val = (byte)((val << 1) | (oldC ? 1 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 6; break; }
                case 0x2E: { addr = AddrAbsolute(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x80) != 0; val = (byte)((val << 1) | (oldC ? 1 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 6; break; }
                case 0x3E: { AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = _bus.Read(addr); bool oldC = C; C = (val & 0x80) != 0; val = (byte)((val << 1) | (oldC ? 1 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 7; break; }

                // ROR
                case 0x6A: { bool oldC = C; C = (A & 0x01) != 0; A = (byte)((A >> 1) | (oldC ? 0x80 : 0)); SetZN(A); cycles = 2; break; }
                case 0x66: { addr = AddrZeroPage(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x01) != 0; val = (byte)((val >> 1) | (oldC ? 0x80 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 5; break; }
                case 0x76: { addr = AddrZeroPageX(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x01) != 0; val = (byte)((val >> 1) | (oldC ? 0x80 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 6; break; }
                case 0x6E: { addr = AddrAbsolute(); val = _bus.Read(addr); bool oldC = C; C = (val & 0x01) != 0; val = (byte)((val >> 1) | (oldC ? 0x80 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 6; break; }
                case 0x7E: { AddrAbsoluteX(out _); addr = (ushort)(AddrAbsoluteXBase() + X); val = _bus.Read(addr); bool oldC = C; C = (val & 0x01) != 0; val = (byte)((val >> 1) | (oldC ? 0x80 : 0)); _bus.Write(addr, val); SetZN(val); cycles = 7; break; }

                // Jumps & Calls
                case 0x4C: PC = AddrAbsolute(); cycles = 3; break;
                case 0x6C: PC = ReadWordBug(ReadWord()); cycles = 5; break;
                case 0x20:
                    addr = AddrAbsolute();
                    PushWord((ushort)(PC - 1));
                    PC = addr;
                    cycles = 6;
                    break;
                case 0x60: PC = (ushort)(PopWord() + 1); cycles = 6; break;
                case 0x40: Status = Pop(); PC = PopWord(); cycles = 6; break;

                // Branches
                case 0x90: Branch(!C, ref cycles); break;
                case 0xB0: Branch(C, ref cycles); break;
                case 0xF0: Branch(Z, ref cycles); break;
                case 0xD0: Branch(!Z, ref cycles); break;
                case 0x30: Branch(N, ref cycles); break;
                case 0x10: Branch(!N, ref cycles); break;
                case 0x50: Branch(!V, ref cycles); break;
                case 0x70: Branch(V, ref cycles); break;

                // Stack transfers
                case 0x48: Push(A); cycles = 3; break;
                case 0x08: Push((byte)(Status | 0x30)); cycles = 3; break; // PHP pushes B and bit 5 set
                case 0x68: A = Pop(); SetZN(A); cycles = 4; break;
                case 0x28: Status = Pop(); cycles = 4; break;
                case 0xBA: TSX(); cycles = 2; break;
                case 0x9A: TXS(); cycles = 2; break;

                // Register transfers
                case 0xAA: X = A; SetZN(X); cycles = 2; break;
                case 0x8A: A = X; SetZN(A); cycles = 2; break;
                case 0xA8: Y = A; SetZN(Y); cycles = 2; break;
                case 0x98: A = Y; SetZN(A); cycles = 2; break;

                // Flags
                case 0x18: C = false; cycles = 2; break;
                case 0x38: C = true; cycles = 2; break;
                case 0x58: I = false; cycles = 2; break;
                case 0x78: I = true; cycles = 2; break;
                case 0xD8: D = false; cycles = 2; break;
                case 0xF8: D = true; cycles = 2; break;
                case 0xB8: V = false; cycles = 2; break;

                // NOP
                case 0xEA: cycles = 2; break;

                // BRK
                case 0x00:
                    PC++; // BRK skips signature byte
                    PushWord(PC);
                    Push((byte)(Status | 0x30)); // Set B and bit 5
                    I = true;
                    PC = (ushort)(_bus.Read(0xFFFE) | (_bus.Read(0xFFFF) << 8));
                    cycles = 7;
                    break;

                default:
                    // Treat illegal opcodes gracefully as NOPs or handle common ones
                    cycles = 2;
                    break;
            }

            _bus.Tick(cycles);
            TotalCycles += cycles;
            InstructionsExecuted++;
            return cycles;
        }

        private void TSX() { X = SP; SetZN(X); }
        private void TXS() { SP = X; }

        private ushort AddrAbsoluteXBase()
        {
            ushort baseAddr = (ushort)(_bus.Read((ushort)(PC - 2)) | (_bus.Read((ushort)(PC - 1)) << 8));
            return baseAddr;
        }

        private ushort AddrAbsoluteYBase()
        {
            ushort baseAddr = (ushort)(_bus.Read((ushort)(PC - 2)) | (_bus.Read((ushort)(PC - 1)) << 8));
            return baseAddr;
        }

        private ushort AddrIndirectYTarget()
        {
            byte ptr = _bus.Read((ushort)(PC - 1));
            byte lo = _bus.Read(ptr);
            byte hi = _bus.Read((byte)(ptr + 1));
            return (ushort)(((hi << 8) | lo) + Y);
        }
    }
}
