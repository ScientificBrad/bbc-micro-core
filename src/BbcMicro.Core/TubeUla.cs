using System;
using System.Collections.Generic;

namespace BbcMicro.Core
{
    /// <summary>
    /// Acorn Tube ULA (Uncommitted Logic Array) emulation.
    /// Interconnects the BBC Micro Host (MOS 1.20 @ 2MHz) and Parasite Second Processor (65C02 @ 3MHz)
    /// across 4 bidirectional register channels with FIFO/latch semantics, status bits, control flags,
    /// and IRQ/NMI generation.
    /// </summary>
    public class TubeUla
    {
        public const int R1_FIFO_CAPACITY = 24;
        public const int R3_FIFO_CAPACITY = 24;

        // Control Register ($FEE0 Write)
        // Bit 7: Parasite R1 IRQ Enable (P.IRQ)
        // Bit 6: Parasite R4 IRQ Enable (P.IRQ4)
        // Control flags in ControlRegister (7-bit):
        // Bit 6: t_flag (Tube reset - holds FIFOs in reset)
        // Bit 5: p_flag (Parasite reset - held in reset when 1)
        // Bit 4: v_flag (Two-byte mode)
        // Bit 3: m_flag (Parasite NMI enable from HP3 non-empty / PH3 space available)
        // Bit 2: j_flag (Parasite IRQ enable from HP4 non-empty)
        // Bit 1: i_flag (Parasite IRQ enable from HP1 non-empty)
        // Bit 0: q_flag (Host IRQ enable from PH4 non-empty)
        public byte ControlRegister { get; set; } = 0x01; // Power-up: 7'b0000001 (Parasite running, HIRQ enabled)

        public bool ParasiteInReset => (ControlRegister & 0x20) != 0;

        // Channel 1 (R1): 24-byte FIFO Host->Parasite; 1-byte latch Parasite->Host
        private readonly Queue<byte> _r1HostToParasite = new(R1_FIFO_CAPACITY);
        private byte _r1ParasiteToHostData;
        private bool _r1ParasiteToHostHasData;

        // Channel 2 (R2): 1-byte latch Host->Parasite; 1-byte latch Parasite->Host
        private byte _r2HostToParasiteData;
        private bool _r2HostToParasiteHasData;
        private byte _r2ParasiteToHostData;
        private bool _r2ParasiteToHostHasData;

        // Channel 3 (R3): 24-byte FIFO Host->Parasite; 24-byte FIFO Parasite->Host
        private readonly Queue<byte> _r3HostToParasite = new(R3_FIFO_CAPACITY);
        private readonly Queue<byte> _r3ParasiteToHost = new(R3_FIFO_CAPACITY);

        // Channel 4 (R4): 1-byte latch Host->Parasite; 1-byte latch Parasite->Host
        private byte _r4HostToParasiteData;
        private bool _r4HostToParasiteHasData;
        private byte _r4ParasiteToHostData;
        private bool _r4ParasiteToHostHasData;

        // Trace logging
        public bool TraceEnabled { get; set; } = false;
        public Action<string>? OnTrace;

        public TubeUla()
        {
            Reset();
        }

        public void Reset()
        {
            ControlRegister = 0x01; // Power up: 7'b0000001 (q_flag=1, p_flag=0)
            _r1HostToParasite.Clear();
            _r1ParasiteToHostHasData = false;

            _r2HostToParasiteHasData = false;
            _r2ParasiteToHostHasData = false;

            _r3HostToParasite.Clear();
            _r3ParasiteToHost.Clear();

            _r4HostToParasiteHasData = false;
            _r4ParasiteToHostHasData = false;
        }

        // =========================================================================
        // Interrupt Lines
        // =========================================================================

        public bool HostIrq =>
            (ControlRegister & 0x01) != 0 && _r4ParasiteToHostHasData;

        public bool HostNmi => false;

        public bool ParasiteIrq =>
            ((ControlRegister & 0x04) != 0 && _r4HostToParasiteHasData) ||
            ((ControlRegister & 0x02) != 0 && _r1HostToParasite.Count > 0);

        public bool ParasiteNmi =>
            (ControlRegister & 0x08) != 0 && (_r3HostToParasite.Count > 0 || _r3ParasiteToHost.Count < R3_FIFO_CAPACITY);

        public void ClearHostNmi() { }
        public void ClearParasiteNmi() { }

        // =========================================================================
        // Host Status Registers ($FEE0, $FEE2, $FEE4, $FEE6)
        // Bit 7: Read Ready (Parasite-to-Host has data)
        // Bit 6: Write Ready (Host-to-Parasite has space)
        // Bits 5..0: control[5:0] on R1, 6'b111111 on R2..R4
        // =========================================================================

        public byte HostR1Status
        {
            get
            {
                byte res = (byte)(ControlRegister & 0x3F);
                if (_r1ParasiteToHostHasData) res |= 0x80;
                if (_r1HostToParasite.Count < R1_FIFO_CAPACITY) res |= 0x40;
                return res;
            }
        }

        public byte HostR2Status
        {
            get
            {
                byte res = 0x3F;
                if (_r2ParasiteToHostHasData) res |= 0x80;
                if (!_r2HostToParasiteHasData) res |= 0x40;
                return res;
            }
        }

        public byte HostR3Status
        {
            get
            {
                byte res = 0x3F;
                if (_r3ParasiteToHost.Count > 0) res |= 0x80;
                if (_r3HostToParasite.Count < R3_FIFO_CAPACITY) res |= 0x40;
                return res;
            }
        }

        public byte HostR4Status
        {
            get
            {
                byte res = 0x3F;
                if (_r4ParasiteToHostHasData) res |= 0x80;
                if (!_r4HostToParasiteHasData) res |= 0x40;
                return res;
            }
        }

        // =========================================================================
        // Parasite Status Registers ($FEF8, $FEFA, $FEFC, $FEFE)
        // Bit 7: Read Ready (Host-to-Parasite has data)
        // Bit 6: Write Ready (Parasite-to-Host has space)
        // Bits 5..0: control[5:0] on R1, 6'b111111 on R2..R4
        // =========================================================================

        public byte ParasiteR1Status
        {
            get
            {
                byte res = (byte)(ControlRegister & 0x3F);
                if (_r1HostToParasite.Count > 0) res |= 0x80;
                if (!_r1ParasiteToHostHasData) res |= 0x40;
                return res;
            }
        }

        public byte ParasiteR2Status
        {
            get
            {
                byte res = 0x3F;
                if (_r2HostToParasiteHasData) res |= 0x80;
                if (!_r2ParasiteToHostHasData) res |= 0x40;
                return res;
            }
        }

        public byte ParasiteR3Status
        {
            get
            {
                byte res = 0x3F;
                if (_r3HostToParasite.Count > 0) res |= 0x80;
                if (_r3ParasiteToHost.Count < R3_FIFO_CAPACITY) res |= 0x40;
                return res;
            }
        }

        public byte ParasiteR4Status
        {
            get
            {
                byte res = 0x3F;
                if (_r4HostToParasiteHasData) res |= 0x80;
                if (!_r4ParasiteToHostHasData) res |= 0x40;
                return res;
            }
        }

        // =========================================================================
        // Host Bus Interface ($FEE0 - $FEE7)
        // =========================================================================

        public byte HostRead(ushort address)
        {
            int reg = address & 0x07;
            byte val = 0;
            switch (reg)
            {
                case 0: // R1STAT
                    val = HostR1Status;
                    break;
                case 1: // R1DATA
                    val = _r1ParasiteToHostData;
                    _r1ParasiteToHostHasData = false;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R1 READ] {val:X2}");
                    break;
                case 2: // R2STAT
                    val = HostR2Status;
                    break;
                case 3: // R2DATA
                    val = _r2ParasiteToHostData;
                    _r2ParasiteToHostHasData = false;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R2 READ] {val:X2}");
                    break;
                case 4: // R3STAT
                    val = HostR3Status;
                    break;
                case 5: // R3DATA
                    if (_r3ParasiteToHost.Count > 0)
                        val = _r3ParasiteToHost.Dequeue();
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R3 READ] {val:X2}");
                    break;
                case 6: // R4STAT
                    val = HostR4Status;
                    break;
                case 7: // R4DATA
                    val = _r4ParasiteToHostData;
                    _r4ParasiteToHostHasData = false;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R4 READ] {val:X2}");
                    break;
            }
            return val;
        }

        public void HostWrite(ushort address, byte value)
        {
            int reg = address & 0x07;
            switch (reg)
            {
                case 0: // Control Register / R1STAT
                    if ((value & 0x80) != 0)
                        ControlRegister |= (byte)(value & 0x7F);
                    else
                        ControlRegister &= (byte)~(value & 0x7F);
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST CTRL WRITE] {value:X2} (Ctrl={ControlRegister:X2}, PRST={(ParasiteInReset ? 1 : 0)})");
                    break;
                case 1: // R1DATA
                    if (_r1HostToParasite.Count < R1_FIFO_CAPACITY)
                        _r1HostToParasite.Enqueue(value);
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R1 WRITE] {value:X2} (Queue={_r1HostToParasite.Count})");
                    break;
                case 2: // R2STAT (writes ignored or control)
                    break;
                case 3: // R2DATA
                    _r2HostToParasiteData = value;
                    _r2HostToParasiteHasData = true;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R2 WRITE] {value:X2}");
                    break;
                case 4: // R3STAT
                    break;
                case 5: // R3DATA
                    if (_r3HostToParasite.Count < R3_FIFO_CAPACITY)
                        _r3HostToParasite.Enqueue(value);
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R3 WRITE] {value:X2} (Queue={_r3HostToParasite.Count})");
                    break;
                case 6: // R4STAT
                    break;
                case 7: // R4DATA
                    _r4HostToParasiteData = value;
                    _r4HostToParasiteHasData = true;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE HOST R4 WRITE] {value:X2}");
                    break;
            }
        }

        // =========================================================================
        // Parasite Bus Interface ($FEF8 - $FEFF)
        // =========================================================================

        public byte ParasiteRead(ushort address)
        {
            int reg = address & 0x07;
            byte val = 0;
            switch (reg)
            {
                case 0: // R1STAT
                    val = ParasiteR1Status;
                    break;
                case 1: // R1DATA
                    if (_r1HostToParasite.Count > 0)
                        val = _r1HostToParasite.Dequeue();
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R1 READ] {val:X2}");
                    break;
                case 2: // R2STAT
                    val = ParasiteR2Status;
                    break;
                case 3: // R2DATA
                    val = _r2HostToParasiteData;
                    _r2HostToParasiteHasData = false;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R2 READ] {val:X2}");
                    break;
                case 4: // R3STAT
                    val = ParasiteR3Status;
                    break;
                case 5: // R3DATA
                    if (_r3HostToParasite.Count > 0)
                        val = _r3HostToParasite.Dequeue();
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R3 READ] {val:X2}");
                    break;
                case 6: // R4STAT
                    val = ParasiteR4Status;
                    break;
                case 7: // R4DATA
                    val = _r4HostToParasiteData;
                    _r4HostToParasiteHasData = false;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R4 READ] {val:X2}");
                    break;
            }
            return val;
        }

        public void ParasiteWrite(ushort address, byte value)
        {
            int reg = address & 0x07;
            switch (reg)
            {
                case 0: // R1STAT
                    break;
                case 1: // R1DATA
                    _r1ParasiteToHostData = value;
                    _r1ParasiteToHostHasData = true;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R1 WRITE] {value:X2}");
                    break;
                case 2: // R2STAT
                    break;
                case 3: // R2DATA
                    _r2ParasiteToHostData = value;
                    _r2ParasiteToHostHasData = true;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R2 WRITE] {value:X2}");
                    break;
                case 4: // R3STAT
                    break;
                case 5: // R3DATA
                    if (_r3ParasiteToHost.Count < R3_FIFO_CAPACITY)
                        _r3ParasiteToHost.Enqueue(value);
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R3 WRITE] {value:X2}");
                    break;
                case 6: // R4STAT
                    break;
                case 7: // R4DATA
                    _r4ParasiteToHostData = value;
                    _r4ParasiteToHostHasData = true;
                    if (TraceEnabled) OnTrace?.Invoke($"[TUBE PARA R4 WRITE] {value:X2}");
                    break;
            }
        }
    }
}
