using System;
using System.IO;
using BbcMicro.Core;

class GameProgressTest
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");

        // TEST SNAPPER
        Console.WriteLine("================ TESTING SNAPPER ================");
        var snapMachine = new BbcMachine(osRom, basicRom, dfsRom);
        snapMachine.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc001-SnapperV2.ssd"));
        snapMachine.RunUntilPrompt(">", 4_000_000);
        snapMachine.TypeCommand("*EXEC !BOOT");
        snapMachine.RunCycles(15_000_000); // Let it load past SNAPPER and Snap2

        byte snapMode = snapMachine.Bus.Ram[0x0355];
        Console.WriteLine($"Snapper Mode: {snapMode}, PC=${snapMachine.Cpu.PC:X4}");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(snapMachine.Bus.Ram));

        Console.WriteLine("Pressing Space in Snapper...");
        snapMachine.Bus.Keyboard.SetKeyDown(0x62, true);
        snapMachine.RunCycles(500_000);
        snapMachine.Bus.Keyboard.SetKeyDown(0x62, false);
        snapMachine.RunCycles(10_000_000);

        snapMode = snapMachine.Bus.Ram[0x0355];
        Console.WriteLine($"After Space Snapper Mode: {snapMode}, PC=${snapMachine.Cpu.PC:X4}");
        if (snapMode == 7)
            Console.WriteLine(TeletextRenderer.RenderScreenToString(snapMachine.Bus.Ram));

        // TEST ELITE
        Console.WriteLine("\n================ TESTING ELITE ================");
        var eliteMachine = new BbcMachine(osRom, basicRom, dfsRom);
        eliteMachine.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc021-EliteD.ssd"));
        eliteMachine.RunUntilPrompt(">", 4_000_000);
        eliteMachine.TypeCommand("*EXEC !BOOT");
        eliteMachine.RunCycles(12_000_000);

        Console.WriteLine("Pressing 'Y' in Elite...");
        eliteMachine.Bus.Keyboard.SetKeyDown(0x44, true);
        eliteMachine.RunCycles(500_000);
        eliteMachine.Bus.Keyboard.SetKeyDown(0x44, false);
        eliteMachine.RunCycles(6_000_000);

        byte eliteMode = eliteMachine.Bus.Ram[0x0355];
        Console.WriteLine($"Elite Mode after Y: {eliteMode}, PC=${eliteMachine.Cpu.PC:X4}");
        Console.WriteLine(TeletextRenderer.RenderScreenToString(eliteMachine.Bus.Ram));

        Console.WriteLine("Pressing Space in Elite...");
        eliteMachine.Bus.Keyboard.SetKeyDown(0x62, true);
        eliteMachine.RunCycles(500_000);
        eliteMachine.Bus.Keyboard.SetKeyDown(0x62, false);
        eliteMachine.RunCycles(15_000_000);

        eliteMode = eliteMachine.Bus.Ram[0x0355];
        Console.WriteLine($"After Space Elite Mode: {eliteMode}, PC=${eliteMachine.Cpu.PC:X4}");
        if (eliteMode == 7)
            Console.WriteLine(TeletextRenderer.RenderScreenToString(eliteMachine.Bus.Ram));
    }
}
