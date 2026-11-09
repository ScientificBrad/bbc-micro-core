using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BbcMicro.Core;

namespace BbcMicro.Emulator
{
    internal static class MatrixDemo
    {
        private const int Mode7Base = 0x7C00;
        private const int ScreenBytes = 40 * 25;

        public static void Run(BbcMachine machine, bool headless, int frames)
        {
            string programPath = Path.Combine("games", "MATRIX.BAS");
            if (!File.Exists(programPath))
                throw new FileNotFoundException("BBC BASIC Matrix source not found", programPath);

            // Feed the original BASIC interpreter just as a human would type
            // numbered lines. Neither the host nor the renderer creates rain.
            foreach (string raw in File.ReadLines(programPath))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                machine.TypeCommand(line);
                machine.RunCycles(320_000);
            }

            machine.TypeCommand("RUN");
            machine.RunCycles(1_200_000);

            if (headless)
            {
                for (int i = 0; i < frames; i++)
                    machine.RunCycles(180_000);

                int white = CountAttribute(machine.Bus.Ram, 7);
                int green = CountAttribute(machine.Bus.Ram, 2);
                int glyphs = machine.Bus.Ram.Skip(Mode7Base)
                    .Take(ScreenBytes).Count(b => (b & 0x7f) >= 33 && (b & 0x7f) <= 126);

                Console.WriteLine("\n--- BBC MICRO MATRIX HEADLESS TEST ---");
                Console.WriteLine($"Frames: {frames}, 6502 cycles: {machine.Cpu.TotalCycles:N0}");
                Console.WriteLine($"Mode 7 green attributes: {green}, white head attributes: {white}, printable glyphs: {glyphs}");
                Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

                string tail = machine.CapturedOutput.ToString();
                if (tail.Length > 500) tail = tail[^500..];
                Console.WriteLine("\n--- Last ROM output ---");
                Console.WriteLine(tail.Replace("\r", ""));
                bool passed = white > 0 && green > 0 && glyphs > 25
                    && !tail.Contains("Mistake at line", StringComparison.OrdinalIgnoreCase)
                    && !tail.Contains("No room at line", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(passed ? "MATRIX RUNTIME: PASS (BASIC wrote live screen RAM)"
                    : "MATRIX RUNTIME: FAIL (review ROM output and screen memory)");
                if (!passed) Environment.ExitCode = 1;
                return;
            }

            bool stop = false;
            ConsoleCancelEventHandler cancelHandler = (sender, e) =>
            {
                e.Cancel = true;
                stop = true;
            };
            Console.CancelKeyPress += cancelHandler;

            try
            {
                Console.Write("\u001b[?1049h\u001b[?25l\u001b[2J");
                var clock = Stopwatch.StartNew();
                int drawnFrames = 0;
                while (!stop)
                {
                    machine.RunCycles(180_000);
                    Console.Write("\u001b[H");
                    Console.Write(Mode7AnsiRenderer.Render(machine.Bus.Ram));
                    drawnFrames++;
                    Console.Write("\u001b[0mBBC MICRO MATRIX   REAL 6502 + BASIC II   ESC: EXIT\u001b[K");

                    if (drawnFrames % 25 == 0)
                    {
                        Console.Write($"\u001b[K\n6502 cycles: {machine.Cpu.TotalCycles:N0} | FPS: {drawnFrames / Math.Max(clock.Elapsed.TotalSeconds, 0.01):F1}\u001b[K");
                    }

                    try
                    {
                        if (!Console.IsInputRedirected && Console.KeyAvailable &&
                            Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
                            stop = true;
                    }
                    catch (InvalidOperationException) { /* No attached console input. */ }

                    Thread.Sleep(25);
                }
            }
            finally
            {
                Console.Write("\u001b[0m\u001b[?25h\u001b[?1049l");
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        private static int CountAttribute(byte[] ram, int control)
        {
            return ram.Skip(Mode7Base).Take(ScreenBytes)
                .Count(b => (b & 0x7f) == control);
        }
    }
}
