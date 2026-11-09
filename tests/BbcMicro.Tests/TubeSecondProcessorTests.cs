using System;
using Xunit;
using BbcMicro.Core;

namespace BbcMicro.Tests
{
    public class SimpleBus : IMemoryBus
    {
        public byte[] Ram = new byte[65536];
        public bool IrqPending { get; set; }
        public bool NmiPending { get; set; }
        public void ClearNmi() => NmiPending = false;
        public void Tick(int cycles) { }
        public byte Read(ushort address) => Ram[address];
        public void Write(ushort address, byte value) => Ram[address] = value;
    }

    public class TubeSecondProcessorTests
    {
        [Fact]
        public void Test_65C02_Bra()
        {
            var bus = new SimpleBus();
            // FFFC/D reset vector -> 0x1000
            bus.Ram[0xFFFC] = 0x00; bus.Ram[0xFFFD] = 0x10;
            // 1000: BRA +4 -> jumps to 1006
            bus.Ram[0x1000] = 0x80; bus.Ram[0x1001] = 0x04;
            bus.Ram[0x1002] = 0xEA; // NOP (skipped)
            bus.Ram[0x1003] = 0xEA; // NOP (skipped)
            bus.Ram[0x1004] = 0xEA; // NOP (skipped)
            bus.Ram[0x1005] = 0xEA; // NOP (skipped)
            bus.Ram[0x1006] = 0xA9; bus.Ram[0x1007] = 0x42; // LDA #$42

            var cpu = new Cpu65C02(bus);
            cpu.Step(); // executes BRA
            Assert.Equal(0x1006, cpu.PC);
            cpu.Step(); // executes LDA #$42
            Assert.Equal(0x42, cpu.A);
        }

        [Fact]
        public void Test_65C02_Phx_Plx_Phy_Ply_IncA_DecA()
        {
            var bus = new SimpleBus();
            bus.Ram[0xFFFC] = 0x00; bus.Ram[0xFFFD] = 0x10;
            // LDX #$35; PHX; LDX #$00; PLX
            // LDY #$79; PHY; LDY #$00; PLY
            // LDA #$10; INC A; DEC A; DEC A
            byte[] prg = new byte[]
            {
                0xA2, 0x35,       // LDX #$35
                0xDA,             // PHX
                0xA2, 0x00,       // LDX #$00
                0xFA,             // PLX
                0xA0, 0x79,       // LDY #$79
                0x5A,             // PHY
                0xA0, 0x00,       // LDY #$00
                0x7A,             // PLY
                0xA9, 0x10,       // LDA #$10
                0x1A,             // INC A -> $11
                0x3A,             // DEC A -> $10
                0x3A              // DEC A -> $0F
            };
            Buffer.BlockCopy(prg, 0, bus.Ram, 0x1000, prg.Length);

            var cpu = new Cpu65C02(bus);
            for (int i = 0; i < 12; i++) cpu.Step();

            Assert.Equal(0x35, cpu.X);
            Assert.Equal(0x79, cpu.Y);
            Assert.Equal(0x0F, cpu.A);
        }

        [Fact]
        public void Test_65C02_Stz_Tsb_Trb()
        {
            var bus = new SimpleBus();
            bus.Ram[0xFFFC] = 0x00; bus.Ram[0xFFFD] = 0x10;
            bus.Ram[0x0050] = 0xFF;
            bus.Ram[0x0060] = 0x0F;

            // STZ $50
            // LDA #$F0; TSB $60 (tests $0F & $F0 = 0 -> Z=1; $60 becomes $FF)
            // LDA #$0F; TRB $60 (tests $FF & $0F != 0 -> Z=0; $60 becomes $F0)
            byte[] prg = new byte[]
            {
                0x64, 0x50,       // STZ $50
                0xA9, 0xF0,       // LDA #$F0
                0x04, 0x60,       // TSB $60
                0xA9, 0x0F,       // LDA #$0F
                0x14, 0x60        // TRB $60
            };
            Buffer.BlockCopy(prg, 0, bus.Ram, 0x1000, prg.Length);

            var cpu = new Cpu65C02(bus);
            cpu.Step(); // STZ $50
            Assert.Equal(0x00, bus.Ram[0x0050]);

            cpu.Step(); // LDA #$F0
            cpu.Step(); // TSB $60
            Assert.True(cpu.Z);
            Assert.Equal(0xFF, bus.Ram[0x0060]);

            cpu.Step(); // LDA #$0F
            cpu.Step(); // TRB $60
            Assert.False(cpu.Z);
            Assert.Equal(0xF0, bus.Ram[0x0060]);
        }

        [Fact]
        public void Test_65C02_ZeroPageIndirect()
        {
            var bus = new SimpleBus();
            bus.Ram[0xFFFC] = 0x00; bus.Ram[0xFFFD] = 0x10;
            // Pointer at $20 points to $2000
            bus.Ram[0x0020] = 0x00; bus.Ram[0x0021] = 0x20;
            bus.Ram[0x2000] = 0x77;

            // LDA ($20) -> loads $77 into A
            // STA ($20) with A=$88 -> stores $88 to $2000
            byte[] prg = new byte[]
            {
                0xB2, 0x20,       // LDA ($20)
                0xA9, 0x88,       // LDA #$88
                0x92, 0x20        // STA ($20)
            };
            Buffer.BlockCopy(prg, 0, bus.Ram, 0x1000, prg.Length);

            var cpu = new Cpu65C02(bus);
            cpu.Step();
            Assert.Equal(0x77, cpu.A);
            cpu.Step();
            cpu.Step();
            Assert.Equal(0x88, bus.Ram[0x2000]);
        }

        [Fact]
        public void Test_TubeUla_Channel1_FifoAndBackpressure()
        {
            var tube = new TubeUla();
            // Initially, Host can write (Bit 6 set), Parasite has nothing to read (Bit 7 clear)
            Assert.True((tube.HostR1Status & 0x40) != 0);
            Assert.False((tube.ParasiteR1Status & 0x80) != 0);

            // Fill R1 FIFO (24 bytes) from Host
            for (int i = 0; i < 24; i++)
            {
                tube.HostWrite(0xFEE1, (byte)(i + 1));
            }

            // FIFO should now be full: Host Bit 6 = 0 (no space)
            Assert.False((tube.HostR1Status & 0x40) != 0);
            // Parasite should have data: Parasite Bit 7 = 1 (read ready)
            Assert.True((tube.ParasiteR1Status & 0x80) != 0);

            // Drain FIFO from Parasite
            for (int i = 0; i < 24; i++)
            {
                byte val = tube.ParasiteRead(0xFEF9);
                Assert.Equal((byte)(i + 1), val);
            }

            // FIFO should now be empty: Parasite Bit 7 = 0, Host Bit 6 = 1
            Assert.False((tube.ParasiteR1Status & 0x80) != 0);
            Assert.True((tube.HostR1Status & 0x40) != 0);
        }

        [Fact]
        public void Test_TubeUla_Channel3_FifoAndControlReg()
        {
            var tube = new TubeUla();
            tube.HostWrite(0xFEE0, 0x80 | 0x18); // Control register bit-set write
            Assert.Equal(0x19, tube.ControlRegister);
            Assert.False(tube.ParasiteInReset);

            // Host streams into R3
            tube.HostWrite(0xFEE5, 0xAA);
            tube.HostWrite(0xFEE5, 0xBB);

            Assert.True((tube.ParasiteR3Status & 0x80) != 0); // Read ready
            Assert.Equal(0xAA, tube.ParasiteRead(0xFEFD));
            Assert.Equal(0xBB, tube.ParasiteRead(0xFEFD));
            Assert.False((tube.ParasiteR3Status & 0x80) != 0); // Empty now
        }

        [Fact]
        public void Test_DualCpu_InterleavedScheduler()
        {
            var machine = new BbcMachine();
            machine.EnableTube();

            // Check tube enabled
            Assert.True(machine.TubeEnabled);
            Assert.NotNull(machine.ParasiteCpu);
            Assert.NotNull(machine.Tube);

            // Reset Tube PRST so parasite can run
            machine.Tube.HostWrite(0xFEE0, 0x00); // PRST = 0

            // Put a NOP loop in parasite memory at reset vector
            machine.ParasiteBus!.Ram[0xFFFC] = 0x00;
            machine.ParasiteBus!.Ram[0xFFFD] = 0x10;
            machine.ParasiteBus!.Ram[0x1000] = 0xEA; // NOP
            machine.ParasiteBus!.Ram[0x1001] = 0x4C; // JMP $1000
            machine.ParasiteBus!.Ram[0x1002] = 0x00;
            machine.ParasiteBus!.Ram[0x1003] = 0x10;

            machine.ParasiteCpu!.Reset();

            long initialParasiteCycles = machine.ParasiteCpu.TotalCycles;
            machine.RunCycles(1000); // 1000 host cycles

            // Parasite runs at 3 MHz vs Host at 2 MHz (ratio 1.5x)
            long parasiteCyclesRan = machine.ParasiteCpu.TotalCycles - initialParasiteCycles;
            Assert.True(parasiteCyclesRan >= 1450 && parasiteCyclesRan <= 1550,
                $"Expected ~1500 parasite cycles for 1000 host cycles, got {parasiteCyclesRan}");
        }

        private static string FindFile(string relativePath)
        {
            string? dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = System.IO.Path.Combine(dir, relativePath);
                if (System.IO.File.Exists(candidate)) return candidate;
                dir = System.IO.Directory.GetParent(dir)?.FullName;
            }
            throw new System.IO.FileNotFoundException($"Cannot find file: {relativePath}");
        }

        [Fact]
        public void Test_ChuckieEgg2023_BootTrace()
        {
            byte[] osRom = System.IO.File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = System.IO.File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = System.IO.File.ReadAllBytes(FindFile("roms/dfs09.rom"));
            byte[] disk = System.IO.File.ReadAllBytes(FindFile("games/Disc999-chuckieEgg2023Web.ssd"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.EnableTube();
            machine.Bus.Fdc.Drive0.LoadImage(disk);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute */Boot directly
            machine.TypeCommand("*/Boot");

            // Trace execution of $.Boot
            int bootSteps = 0;
            ushort lastPc = 0;
            while (machine.Cpu.TotalCycles < 15_000_000 && bootSteps < 50_000)
            {
                if (machine.Cpu.PC >= 0x6000 && machine.Cpu.PC < 0x7000)
                {
                    bootSteps++;
                    lastPc = machine.Cpu.PC;
                }
                machine.Step();
            }

            Assert.True(bootSteps > 0, $"Expected $.Boot to execute, but bootSteps was {bootSteps}, last PC=${lastPc:X4}");
        }

        [Fact]
        public void Test_ChuckieEgg2023_TubeHandshakeAndTransfer()
        {
            byte[] osRom = System.IO.File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = System.IO.File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = System.IO.File.ReadAllBytes(FindFile("roms/dfs09.rom"));
            byte[] disk = System.IO.File.ReadAllBytes(FindFile("games/Disc999-chuckieEgg2023Web.ssd"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.EnableTube();
            machine.Bus.Fdc.Drive0.LoadImage(disk);

            machine.RunUntilPrompt(">", 4_000_000);

            var milestones = new System.Collections.Generic.List<string>();
            milestones.Add($"After RunUntilPrompt: Ram[0x0279]=0x{machine.Bus.Ram[0x0279]:X2}, TubeEnabled={machine.TubeEnabled}");
            machine.TypeCommand("*/Boot");

            int r3Writes = 0;
            int r1Writes = 0;
            int r4Writes = 0;
            int r4Reads = 0;
            bool hitBoot = false;
            bool hitTubeReset = false;
            bool hitGameOpen = false;

            for (int i = 0; i < 40_000_000; i++)
            {
                if (machine.Cpu.PC == 0xDB38)
                {
                    milestones.Add($"Hit MOS $DB38 (Tube detect write) at cycle {machine.Cpu.TotalCycles}");
                }
                if (machine.Cpu.PC == 0xDB3D)
                {
                    milestones.Add($"Hit MOS $DB3D (Tube detect read) at cycle {machine.Cpu.TotalCycles}: A=0x{machine.Cpu.A:X2}");
                }
                if (machine.Cpu.PC == 0xDB43)
                {
                    milestones.Add($"Hit MOS $DB43 (Tube detected success!) at cycle {machine.Cpu.TotalCycles}");
                }
                if (machine.Cpu.PC == 0xDB4D)
                {
                    milestones.Add($"Hit MOS $DB4D at cycle {machine.Cpu.TotalCycles}: $0279=0x{machine.Bus.Ram[0x0279]:X2}");
                }

                if (!hitBoot && machine.Cpu.PC == 0x6000)
                {
                    hitBoot = true;
                    milestones.Add($"Hit $.Boot entry at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x692B)
                {
                    milestones.Add($"Hit $692B (OSBYTE 129 check) at cycle {machine.Cpu.TotalCycles}: A=0x{machine.Cpu.A:X2}, X=0x{machine.Cpu.X:X2}, Y=0x{machine.Cpu.Y:X2}");
                }

                if (machine.Cpu.PC == 0x6934)
                {
                    milestones.Add($"Returned from OSBYTE 129 at cycle {machine.Cpu.TotalCycles}: A=0x{machine.Cpu.A:X2}, X=0x{machine.Cpu.X:X2}, Y=0x{machine.Cpu.Y:X2}, Carry={machine.Cpu.C}");
                }

                if (machine.Cpu.PC == 0x6942)
                {
                    milestones.Add($"Hit $6942 (OSBYTE 0 check) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x6949)
                {
                    milestones.Add($"Returned from OSBYTE 0 at cycle {machine.Cpu.TotalCycles}: X=0x{machine.Cpu.X:X2}");
                }

                if (machine.Cpu.PC == 0x6940)
                {
                    milestones.Add($"Hit $6940 (SEC; RTS abort) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x694D)
                {
                    milestones.Add($"Hit $694D (CLC; RTS success) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x69C5)
                {
                    milestones.Add($"Hit $69C5 (Entry 1 copy routine) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x699F)
                {
                    ushort osbyteVec = (ushort)(machine.Bus.Ram[0x020A] | (machine.Bus.Ram[0x020B] << 8));
                    milestones.Add($"Hit $699F (OSBYTE &EA Tube check) at cycle {machine.Cpu.TotalCycles}, Ram[0x0279]=0x{machine.Bus.Ram[0x0279]:X2}, A=0x{machine.Cpu.A:X2}, X=0x{machine.Cpu.X:X2}, Y=0x{machine.Cpu.Y:X2}, OSBYTE_VEC=0x{osbyteVec:X4}");
                    
                    // Trace every instruction until PC == 0x69A8
                    int traceSteps = 0;
                    while (traceSteps < 150)
                    {
                        milestones.Add($"  [OSBYTE trace] PC=${machine.Cpu.PC:X4}, A=${machine.Cpu.A:X2}, X=${machine.Cpu.X:X2}, Y=${machine.Cpu.Y:X2}, SP=${machine.Cpu.SP:X2}, Status=${machine.Cpu.Status:X2}");
                        machine.Step();
                        traceSteps++;
                        if (machine.Cpu.PC == 0x69A8)
                        {
                            milestones.Add($"  [OSBYTE trace finished at 0x69A8] A=${machine.Cpu.A:X2}, X=${machine.Cpu.X:X2}, Y=${machine.Cpu.Y:X2}");
                            break;
                        }
                    }
                }

                if (machine.Cpu.PC == 0x69A8)
                {
                    milestones.Add($"Returned from OSBYTE &EA at cycle {machine.Cpu.TotalCycles}: X=0x{machine.Cpu.X:X2}, Ram[0x0279]=0x{machine.Bus.Ram[0x0279]:X2}");
                }

                if (machine.Cpu.PC == 0x69AC)
                {
                    milestones.Add($"Hit $69AC (SEC; RTS No Tube abort) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x69AE)
                {
                    milestones.Add($"Hit $69AE (CLC; RTS Tube detected) at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x69E9 || machine.Cpu.PC == 0x69ED)
                {
                    milestones.Add($"Hit $69ED (Entry 2 Tube bootstrap) at cycle {machine.Cpu.TotalCycles}");
                }

                if (!hitTubeReset && machine.Cpu.PC == 0x69EF)
                {
                    hitTubeReset = true;
                    milestones.Add($"Hit Tube reset ($69EF) at cycle {machine.Cpu.TotalCycles}");
                }

                if (!hitGameOpen && machine.Cpu.PC == 0x6791)
                {
                    hitGameOpen = true;
                    milestones.Add($"Hit OSFIND open 'Game' at cycle {machine.Cpu.TotalCycles}");
                }

                if (machine.Cpu.PC == 0x6BC7) // STA $FEE7 (R4 write)
                {
                    r4Writes++;
                    if (r4Writes <= 10)
                        milestones.Add($"Host R4 write #{r4Writes}: val=0x{machine.Cpu.A:X2}");
                }

                if (machine.Cpu.PC == 0x6BBE) // STA $FEE5 (R3 write)
                {
                    r3Writes++;
                    if (r3Writes >= 290 && r3Writes <= 305)
                    {
                        var pbus = machine.ParasiteBus!;
                        milestones.Add($"Host R3 write #{r3Writes}: val=0x{machine.Cpu.A:X2}, Parasite PC=0x{machine.ParasiteCpu?.PC:X4}, P.A=0x{machine.ParasiteCpu?.A:X2}, P.$15=0x{pbus.Ram[0x15]:X2}, P.$16=0x{pbus.Ram[0x16]:X2}");
                    }
                }

                if (machine.Cpu.PC == 0x6BAC) // STA $FEE1 (R1 write)
                {
                    r1Writes++;
                }

                if (machine.Cpu.PC == 0x6BD0) // BIT $FEE7 (R4 read)
                {
                    r4Reads++;
                    if (r4Reads <= 10)
                        milestones.Add($"Host R4 read #{r4Reads}");
                }

                machine.Step();

                if (hitGameOpen && r3Writes == 566 && r1Writes == 702 && machine.CapturedOutput.ToString().Contains("Loading"))
                {
                    if (machine.ParasiteBus?.Ram[0xFFFE] == 0x54)
                        break;
                }
            }

            // Verify Chuckie Egg 2023 Tube bootstrap and protocol milestones
            Assert.True(hitTubeReset, "Tube reset sequence was executed");
            Assert.True(hitGameOpen, "OSFIND 'Game' was executed");
            Assert.Equal(566, r3Writes);
            Assert.Equal(702, r1Writes);
            Assert.True(r4Writes >= 10, "R4 handshake command bytes were written");
            Assert.Contains("Loading", machine.CapturedOutput.ToString());

            // Verify Parasite unpack and vector configuration
            Assert.NotNull(machine.ParasiteBus);
            ushort irqVec = (ushort)(machine.ParasiteBus.Ram[0xFFFE] | (machine.ParasiteBus.Ram[0xFFFF] << 8));
            ushort nmiVec = (ushort)(machine.ParasiteBus.Ram[0xFFFA] | (machine.ParasiteBus.Ram[0xFFFB] << 8));
            Assert.Equal(0xFE54, irqVec);
            Assert.Equal(0xFE65, nmiVec);
        }
    }
}
