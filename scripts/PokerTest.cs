using System;
using System.IO;
using BbcMicro.Core;

class PokerTest
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");
        byte[] diskBytes = File.ReadAllBytes("games/Disc999-StripPoker.ssd");

        var machine = new BbcMachine(osRom, basicRom, dfsRom);
        machine.Bus.Fdc.Drive0.LoadImage(diskBytes);

        Console.WriteLine("Booting MOS 1.20...");
        machine.RunUntilPrompt(">", 4_000_000);

        Console.WriteLine("Executing *EXEC !BOOT...");
        machine.TypeCommand("*EXEC !BOOT");
        machine.RunCycles(12_000_000);

        Console.WriteLine("Screen state before Space:");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

        Console.WriteLine("Pressing Space (IKN 0x62)...");
        machine.Bus.Keyboard.SetKeyDown(0x62, true);
        machine.RunCycles(500_000);
        machine.Bus.Keyboard.SetKeyDown(0x62, false);
        machine.RunCycles(10_000_000);

        Console.WriteLine("Screen state after Space:");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
        byte mode = machine.Bus.Ram[0x0355];
        Console.WriteLine($"Active MODE: {mode}");
    }
}
