using System;
using System.IO;
using BbcMicro.Core;

namespace BbcMicro.Emulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Acorn BBC Micro Model B - MOS 1.20 & BASIC II (Project 003)";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            bool headless = false;
            string? execCommand = null;
            string romDir = "roms";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--test" || args[i] == "--headless")
                {
                    headless = true;
                }
                else if (args[i] == "--exec" && i + 1 < args.Length)
                {
                    execCommand = args[++i];
                }
                else if (args[i] == "--romdir" && i + 1 < args.Length)
                {
                    romDir = args[++i];
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

            Console.WriteLine($"Loaded MOS 1.20: {osRom.Length:N0} bytes from {osPath}");
            Console.WriteLine($"Loaded BBC BASIC II: {basicRom.Length:N0} bytes from {basicPath}");
            if (dfsRom != null)
                Console.WriteLine($"Loaded Acorn DFS 0.90: {dfsRom.Length:N0} bytes from {dfsPath}");

            var machine = new BbcMachine(osRom, basicRom, dfsRom);

            Console.WriteLine($"\nReset Vector: ${machine.Cpu.PC:X4}");
            Console.WriteLine("Booting Acorn MOS 1.20...\n");

            // Hook live character output
            machine.Bus.OnCharacterWritten = (c) =>
            {
                Console.Write(c);
            };

            // Run until boot completes and prompt appears
            machine.RunUntilPrompt(">", 4_000_000);

            Console.WriteLine($"\n[BOOT COMPLETE] Cycles: {machine.Cpu.TotalCycles:N0}, Instructions: {machine.Cpu.InstructionsExecuted:N0}");
            Console.WriteLine($"CPU State: PC=${machine.Cpu.PC:X4}, A=${machine.Cpu.A:X2}, X=${machine.Cpu.X:X2}, Y=${machine.Cpu.Y:X2}, SP=${machine.Cpu.SP:X2}, P=${machine.Cpu.Status:X2}");
            Console.WriteLine($"Active Sideways Slot: {machine.Bus.SelectedSlot}");

            Console.WriteLine("\n--- Mode 7 Screen Memory Buffer ($7C00-$7FFF) ---");
            Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

            if (!string.IsNullOrEmpty(execCommand))
            {
                Console.WriteLine($"\nInjecting command: {execCommand}");
                machine.TypeCommand(execCommand);
                machine.RunCycles(1_500_000);

                Console.WriteLine("\n--- Screen After Command Execution ---");
                Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
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
