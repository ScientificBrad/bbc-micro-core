using System;
using System.IO;
using BbcMicro.Core;

class EliteTest
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");
        byte[] diskBytes = File.ReadAllBytes("games/Disc021-EliteD.ssd");

        var machine = new BbcMachine(osRom, basicRom, dfsRom);
        machine.Bus.Fdc.Drive0.LoadImage(diskBytes);

        Console.WriteLine("Booting MOS 1.20...");
        machine.RunUntilPrompt(">", 4_000_000);

        Console.WriteLine("Executing *EXEC !BOOT...");
        machine.TypeCommand("*EXEC !BOOT");
        machine.RunCycles(12_000_000);

        Console.WriteLine("Screen state at Y/N prompt:");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

        // Let's see what happens if we press 'Y'
        Console.WriteLine("Pressing 'Y' (IKN 0x44)...");
        machine.Bus.Keyboard.SetKeyDown(0x44, true);
        machine.RunCycles(500_000);
        machine.Bus.Keyboard.SetKeyDown(0x44, false);
        machine.RunCycles(15_000_000);

        Console.WriteLine("Screen state after 'Y':");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
        byte mode = machine.Bus.Ram[0x0355];
        Console.WriteLine($"Active MODE: {mode}");
    }
}
