using System;
using System.IO;
using BbcMicro.Core;

class ExileTest
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");

        var machine = new BbcMachine(osRom, basicRom, dfsRom);
        machine.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc040-ExileR.ssd"));

        machine.RunUntilPrompt(">", 4_000_000);
        machine.TypeCommand("*EXEC !BOOT");
        machine.RunCycles(12_000_000);

        byte mode = machine.Bus.Ram[0x0355];
        ushort crtcStart = (ushort)(machine.Bus.Crtc.ScreenStartAddress * 8);
        Console.WriteLine($"Exile Mode: {mode}, CRTC ScreenStartAddress=${machine.Bus.Crtc.ScreenStartAddress:X4}, crtcStart=${crtcStart:X4}");

        // Check if ram[0x7C00] has characters
        int nonZero7C00 = 0;
        for (int i = 0x7C00; i < 0x8000; i++)
        {
            if (machine.Bus.Ram[i] != 0 && machine.Bus.Ram[i] != 32) nonZero7C00++;
        }
        Console.WriteLine($"Non-space bytes in $7C00-$7FFF: {nonZero7C00}");

        int nonZeroCrtc = 0;
        for (int i = 0; i < 1024; i++)
        {
            int addr = (crtcStart + i) % 32768;
            if (machine.Bus.Ram[addr] != 0 && machine.Bus.Ram[addr] != 32) nonZeroCrtc++;
        }
        Console.WriteLine($"Non-space bytes in crtcStart=${crtcStart:X4}: {nonZeroCrtc}");
    }
}
