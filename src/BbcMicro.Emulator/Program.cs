using System;
using System.IO;
using BbcMicro.Core;

namespace BbcMicro.Emulator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Acorn BBC Micro Model B - MOS 1.20 & BASIC II (Project 003)";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            bool headless = false;
            bool openWindow = false;
            int windowScale = 2;
            string? execCommand = null;
            string romDir = "roms";
            string? diskPath = null;
            bool autoboot = false;
            bool traceFdc = false;
            bool enableTube = false;
            string? tubeRomPath = null;
            string? dnfsRomPath = null;
            bool traceTube = false;
            bool demoMatrix = false;
            int demoFrames = 60;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--test" || args[i] == "--headless")
                {
                    headless = true;
                }
                else if (args[i] == "--window" || args[i] == "-w" || args[i] == "--gui")
                {
                    openWindow = true;
                }
                else if (args[i] == "--scale" && i + 1 < args.Length && int.TryParse(args[i + 1], out int s))
                {
                    windowScale = Math.Clamp(s, 1, 3);
                    i++;
                }
                else if (args[i] == "--exec" && i + 1 < args.Length)
                {
                    execCommand = args[++i];
                }
                else if (args[i] == "--disk" && i + 1 < args.Length)
                {
                    diskPath = args[++i];
                }
                else if (args[i] == "--autoboot" || args[i] == "--boot")
                {
                    autoboot = true;
                }
                else if (args[i] == "--trace-fdc")
                {
                    traceFdc = true;
                }
                else if (args[i] == "--tube")
                {
                    enableTube = true;
                }
                else if (args[i] == "--tube-rom" && i + 1 < args.Length)
                {
                    tubeRomPath = args[++i];
                    enableTube = true;
                }
                else if (args[i] == "--dnfs-rom" && i + 1 < args.Length)
                {
                    dnfsRomPath = args[++i];
                }
                else if (args[i] == "--trace-tube")
                {
                    traceTube = true;
                }
                else if (args[i] == "--romdir" && i + 1 < args.Length)
                {
                    romDir = args[++i];
                }
                else if (args[i] == "--demo" && i + 1 < args.Length)
                {
                    demoMatrix = args[++i].Equals("matrix", StringComparison.OrdinalIgnoreCase);
                }
                else if (args[i] == "--frames" && i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed))
                {
                    demoFrames = Math.Clamp(parsed, 1, 1000);
                    i++;
                }
            }

            Console.WriteLine("====================================================================");
            Console.WriteLine(" ACORN BBC MICRO MODEL B STANDALONE EMULATOR (1981)");
            Console.WriteLine(" MOS 1.20 | BBC BASIC II | DFS 0.90");
            Console.WriteLine(" Historical Computer Reverse Engineering - Project 003");
            Console.WriteLine("====================================================================\n");

            string osPath = Path.Combine(romDir, "os12.rom");
            string basicPath = Path.Combine(romDir, "basic2.rom");
            string dfsPath = Path.Combine(romDir, "dfs09.rom");

            if (!File.Exists(osPath)) osPath = "os12.rom";
            if (!File.Exists(basicPath)) basicPath = "basic2.rom";
            if (!File.Exists(dfsPath)) dfsPath = "dfs09.rom";

            if (!File.Exists(osPath) || !File.Exists(basicPath))
            {
                Console.WriteLine($"[ERROR] Missing required ROM files (os12.rom or basic2.rom) in '{romDir}' or current directory.");
                return;
            }

            byte[] osRom = File.ReadAllBytes(osPath);
            byte[] basicRom = File.ReadAllBytes(basicPath);
            byte[]? dfsRom = File.Exists(dfsPath) ? File.ReadAllBytes(dfsPath) : null;
            byte[]? dnfsRom = null;
            byte[]? tubeRom = null;

            if (!string.IsNullOrEmpty(dnfsRomPath))
            {
                if (!File.Exists(dnfsRomPath))
                {
                    Console.WriteLine($"[ERROR] DNFS ROM file not found: {dnfsRomPath}");
                    return;
                }
                dnfsRom = File.ReadAllBytes(dnfsRomPath);
                Console.WriteLine($"Loaded Acorn DNFS Host ROM: {dnfsRom.Length:N0} bytes from {dnfsRomPath}");
            }

            if (!string.IsNullOrEmpty(tubeRomPath))
            {
                if (!File.Exists(tubeRomPath))
                {
                    Console.WriteLine($"[ERROR] Tube ROM file not found: {tubeRomPath}");
                    return;
                }
                tubeRom = File.ReadAllBytes(tubeRomPath);
                Console.WriteLine($"Loaded Acorn Tube Client ROM: {tubeRom.Length:N0} bytes from {tubeRomPath}");
            }

            Console.WriteLine($"Loaded MOS 1.20: {osRom.Length:N0} bytes from {osPath}");
            Console.WriteLine($"Loaded BBC BASIC II: {basicRom.Length:N0} bytes from {basicPath}");
            if (dfsRom != null)
                Console.WriteLine($"Loaded Acorn DFS 0.90: {dfsRom.Length:N0} bytes from {dfsPath}");

            var machine = new BbcMachine(osRom, basicRom, dfsRom);

            if (dnfsRom != null)
            {
                // Install DNFS into sideways ROM slot 13
                Buffer.BlockCopy(dnfsRom, 0, machine.Bus.SidewaysRoms[13], 0, Math.Min(dnfsRom.Length, 16384));
            }

            if (enableTube)
            {
                machine.EnableTube(tubeRom);
                Console.WriteLine("[HARDWARE] Acorn 6502/65C02 Second Processor (Tube) Enabled");
                Console.WriteLine("           Host CPU: MOS 6502 @ 2.0 MHz");
                Console.WriteLine("           Parasite CPU: 65C02 @ 3.0 MHz with 64 KB local RAM");
                Console.WriteLine("           Tube ULA: 4 bidirectional register channels ($FEE0-$FEE7)");

                if (traceTube && machine.Tube != null)
                {
                    int traceCount = 0;
                    const int MAX_TRACE = 500;
                    machine.Tube.TraceEnabled = true;
                    machine.Tube.OnTrace = (msg) =>
                    {
                        if (traceCount++ < MAX_TRACE)
                            Console.WriteLine(msg);
                        else if (traceCount == MAX_TRACE + 1)
                            Console.WriteLine("[TUBE TRACE] Maximum trace log entries reached (500). Silencing further trace logs.");
                    };
                }
            }

            if (!string.IsNullOrEmpty(diskPath))
            {
                if (!File.Exists(diskPath))
                {
                    Console.WriteLine($"[ERROR] Disk image not found: {diskPath}");
                    return;
                }
                byte[] diskBytes = File.ReadAllBytes(diskPath);
                using var sha = System.Security.Cryptography.SHA256.Create();
                string diskHash = Convert.ToHexString(sha.ComputeHash(diskBytes)).ToLower();
                Console.WriteLine($"Mounting Drive 0: {diskBytes.Length:N0} bytes from {diskPath}");
                Console.WriteLine($"Disk SHA-256: {diskHash}");
                machine.Bus.Fdc.Drive0.LoadImage(diskBytes);
            }

            if (traceFdc)
            {
                machine.Bus.Fdc.EnableTracing = true;
            }

            Console.WriteLine($"\nReset Vector: ${machine.Cpu.PC:X4}");
            Console.WriteLine("Booting Acorn MOS 1.20...\n");

            // Hook live character output
            machine.Bus.OnCharacterWritten = (c) =>
            {
                // Avoid routing BBC VDU 7 (BEL) to the Windows terminal in GUI mode.
                // Audio belongs to the emulated sound system, not PowerShell's bell.
                if (!openWindow || c != '\a') Console.Write(c);
            };

            // Run until boot completes and prompt appears
            machine.RunUntilPrompt(">", 4_000_000);

            Console.WriteLine($"\n[BOOT COMPLETE] Cycles: {machine.Cpu.TotalCycles:N0}, Instructions: {machine.Cpu.InstructionsExecuted:N0}");
            Console.WriteLine($"CPU State: PC=${machine.Cpu.PC:X4}, A=${machine.Cpu.A:X2}, X=${machine.Cpu.X:X2}, Y=${machine.Cpu.Y:X2}, SP=${machine.Cpu.SP:X2}, P=${machine.Cpu.Status:X2}");
            Console.WriteLine($"Active Sideways Slot: {machine.Bus.SelectedSlot}");

            Console.WriteLine("\n--- Mode 7 Screen Memory Buffer ($7C00-$7FFF) ---");
            Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

            if (demoMatrix)
            {
                MatrixDemo.Run(machine, headless, demoFrames);
                return;
            }

            if (openWindow)
            {
                Console.WriteLine("\n[GUI] Launching native Windows display window (50 Hz real-time 2.0 MHz 6502)...");
                Console.WriteLine("      Controls: [ESC] Exit | [P] Pause/Resume | [F10] Reset | Physical Keyboard mapped to BBC Matrix");
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                string? cmdToInject = autoboot ? "*EXEC !BOOT" : execCommand;
                Application.Run(new BbcMicroWindow(machine, cmdToInject, windowScale));
                return;
            }

            if (autoboot)
            {
                Console.WriteLine("\n[AUTOBOOT] Executing Shift+BREAK / DFS Boot Option 3: *EXEC !BOOT");
                machine.TypeCommand("*EXEC !BOOT");
                machine.RunCycles(5_000_000);

                Console.WriteLine("\n--- Mode 7 Screen Memory Buffer After Autoboot ---");
                Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

                // Save framebuffer screenshot
                uint[] fb = ScreenFramebufferRenderer.RenderMode7(machine.Bus.Ram, machine.Bus.MosRom, 0x7C00, 2);
                string screenPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_screen.bmp");
                BmpWriter.SaveBmp32(screenPath, 640, 500, fb);
                Console.WriteLine($"Saved live framebuffer screenshot: {screenPath}");
            }
            else if (!string.IsNullOrEmpty(execCommand))
            {
                Console.WriteLine($"\nInjecting command: {execCommand}");
                machine.TypeCommand(execCommand);
                machine.RunCycles(8_000_000);

                byte activeMode = machine.Bus.Ram[0x0355];
                Console.WriteLine($"\n--- Screen After Command Execution (Active MODE {activeMode}) ---");
                if (activeMode == 7)
                {
                    Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
                }

                ushort screenStart = (ushort)(machine.Bus.Crtc.ScreenStartAddress * 8);
                uint[] fb = ScreenFramebufferRenderer.RenderScreen(machine.Bus.Ram, activeMode, machine.Bus.MosRom, machine.Bus.Ula.Palette, screenStart);
                string screenPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_1983.bmp");
                BmpWriter.SaveBmp32(screenPath, 640, 512, fb);
                Console.WriteLine($"Saved live framebuffer screenshot: {screenPath}");
            }

            if (headless)
            {
                Console.WriteLine("\nHeadless test run completed successfully.");
                return;
            }

            Console.WriteLine("\n[Press CTRL+C to exit emulator]");
        }
    }
}
