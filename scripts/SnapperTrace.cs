using System;
using System.IO;
using BbcMicro.Core;

class SnapperTrace
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");
        byte[] diskBytes = File.ReadAllBytes("games/Disc001-SnapperV2.ssd");

        var machine = new BbcMachine(osRom, basicRom, dfsRom);
        machine.Bus.Fdc.Drive0.LoadImage(diskBytes);

        machine.RunUntilPrompt(">", 4_000_000);
        Console.WriteLine("At prompt. Executing *EXEC !BOOT...");
        machine.TypeCommand("*EXEC !BOOT");

        for (int i = 1; i <= 10; i++)
        {
            machine.RunCycles(4_000_000);
            byte mode = machine.Bus.Ram[0x0355];
            ushort screenStart = (ushort)(machine.Bus.Crtc.ScreenStartAddress * 8);
            Console.WriteLine($"\n--- After {i * 4}M cycles (MODE {mode}, ScreenStart=${screenStart:X4}, PC=${machine.Cpu.PC:X4}) ---");
            Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
        }
    }
}
