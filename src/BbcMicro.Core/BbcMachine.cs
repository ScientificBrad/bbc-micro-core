using System;
using System.Text;

namespace BbcMicro.Core
{
    public class TeletextRenderer
    {
        public static string RenderScreenToString(byte[] ram, ushort screenBase = 0x7C00)
        {
            var sb = new StringBuilder();
            sb.AppendLine("┌────────────────────────────────────────┐");

            for (int row = 0; row < 25; row++)
            {
                sb.Append("│");
                int rowOffset = screenBase + (row * 40);
                for (int col = 0; col < 40; col++)
                {
                    int addr = rowOffset + col;
                    byte b = (addr < ram.Length) ? (byte)(ram[addr] & 0x7F) : (byte)0x20;
                    if (b < 0x20 || b == 0x7F)
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append((char)b);
                    }
                }
                sb.AppendLine("│");
            }

            sb.AppendLine("└────────────────────────────────────────┘");
            return sb.ToString();
        }
    }

    public class BbcMachine
    {
        public readonly BbcBus Bus;
        public readonly Cpu6502 Cpu;

        public TubeUla? Tube { get; private set; }
        public ParasiteBus? ParasiteBus { get; private set; }
        public Cpu65C02? ParasiteCpu { get; private set; }
        public bool TubeEnabled => Tube != null;

        private long _parasiteCyclesTarget;
        private long _parasiteCyclesExecuted;

        public readonly StringBuilder CapturedOutput = new();
        private readonly System.Collections.Generic.Queue<byte> _directInputQueue = new();

        public BbcMachine(byte[]? mosRom = null, byte[]? basicRom = null, byte[]? dfsRom = null)
        {
            Bus = new BbcBus(mosRom, basicRom, dfsRom);
            Cpu = new Cpu6502(Bus);
        }

        public void EnableTube(byte[]? parasiteBootRom = null)
        {
            Tube = new TubeUla();
            Bus.Tube = Tube;
            ParasiteBus = new ParasiteBus(Tube, parasiteBootRom);
            ParasiteCpu = new Cpu65C02(ParasiteBus);
            Bus.Ram[0x0279] = 0xFF; // Tube presence flag
            Bus.Ram[0x027A] = 0xFF; // OSBYTE &EA Tube flag ($0190 + &EA = $027A)
        }

        public void Reset()
        {
            Bus.Reset();
            Cpu.Reset();
            CapturedOutput.Clear();
            _directInputQueue.Clear();
            _parasiteCyclesTarget = 0;
            _parasiteCyclesExecuted = 0;
            if (Tube != null)
            {
                Tube.Reset();
                ParasiteBus?.Reset();
                ParasiteCpu?.Reset();
                Bus.Ram[0x0279] = 0xFF;
                Bus.Ram[0x027A] = 0xFF;
            }
        }

        public bool InjectKeyboardChar(byte c)
        {
            byte start = Bus.Ram[0x02D9];
            byte end = Bus.Ram[0x02E1];
            byte emptyStart = Bus.Ram[0x02D1]; // 0xE0
            byte next = (byte)(end + 1);
            if (next == 0) next = emptyStart;
            if (next == start) return false; // Buffer full

            Bus.Ram[0x0300 + end] = c;
            Bus.Ram[0x02E1] = next;
            return true;
        }

        public void Step()
        {
            // Drain queue into buffer when possible
            if (_directInputQueue.Count > 0)
            {
                byte nextChar = _directInputQueue.Peek();
                if (InjectKeyboardChar(nextChar))
                {
                    _directInputQueue.Dequeue();
                }
            }

            // Hook WRCH default entry point ($E0A4)
            // This captures all characters emitted via either OSWRCH ($FFEE) or JMP ($020E)
            if (Cpu.PC == 0xE0A4)
            {
                char c = (char)Cpu.A;
                CapturedOutput.Append(c);
                Bus.OnCharacterWritten?.Invoke(c);
            }

            if (TubeEnabled && CapturedOutput.ToString().Contains(">"))
            {
                // Ensure Tube presence flag is set for OSBYTE &EA ($0190 + &EA = $027A)
                Bus.Ram[0x027A] = 0xFF;
                Bus.Ram[0x0279] = 0xFF;
            }

            int hCycles = Cpu.Step();

            if (TubeEnabled && ParasiteCpu != null && Tube != null)
            {
                _parasiteCyclesTarget += (long)hCycles * 3;
                if (!Tube.ParasiteInReset)
                {
                    while (_parasiteCyclesExecuted * 2 < _parasiteCyclesTarget)
                    {
                        int pCycles = ParasiteCpu.Step();
                        _parasiteCyclesExecuted += pCycles;
                    }
                }
                else
                {
                    _parasiteCyclesExecuted = _parasiteCyclesTarget / 2;
                }
            }
        }

        public void RunCycles(long targetCycles)
        {
            long start = Cpu.TotalCycles;
            while ((Cpu.TotalCycles - start) < targetCycles)
            {
                Step();
            }
        }

        public void RunUntilPrompt(string prompt = ">", long maxCycles = 5_000_000)
        {
            long start = Cpu.TotalCycles;
            while ((Cpu.TotalCycles - start) < maxCycles)
            {
                Step();
                if (CapturedOutput.ToString().Contains(prompt))
                    break;
            }
        }

        public void TypeCommand(string command)
        {
            foreach (char c in command)
            {
                _directInputQueue.Enqueue((byte)c);
            }
            _directInputQueue.Enqueue((byte)'\r');
        }
    }
}
