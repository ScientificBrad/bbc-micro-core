using System;
using System.IO;
using BbcMicro.Core;

class SoundCheck
{
    static void Main()
    {
        byte[] osRom = File.ReadAllBytes("roms/os12.rom");
        byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
        byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");
        byte[] diskBytes = File.ReadAllBytes("games/Disc002-ChuckieEgg.ssd");

        var machine = new BbcMachine(osRom, basicRom, dfsRom);
        machine.Bus.Fdc.Drive0.LoadImage(diskBytes);

        Console.WriteLine("Booting MOS 1.20...");
        machine.RunUntilPrompt(">", 4_000_000);

        Console.WriteLine($"Boot complete. Sound write count: {machine.Bus.Sound.WriteCount}");

        Console.WriteLine("Typing *RUN CH_EGG...");
        machine.TypeCommand("*RUN CH_EGG");
        machine.RunCycles(8_000_000);

        Console.WriteLine($"After launch. Sound write count: {machine.Bus.Sound.WriteCount}");
        for (int ch = 0; ch < 3; ch++)
        {
            Console.WriteLine($"  Tone {ch}: Period={machine.Bus.Sound.TonePeriod(ch)}, Vol={machine.Bus.Sound.Attenuation(ch)}");
        }
        Console.WriteLine($"  Noise: Ctrl={machine.Bus.Sound.NoiseControl}, Vol={machine.Bus.Sound.Attenuation(3)}");

        Console.WriteLine("Pressing S to start game...");
        machine.Bus.Keyboard.SetKeyDown(0x51, true); // 'S'
        machine.RunCycles(1_500_000);
        machine.Bus.Keyboard.SetKeyDown(0x51, false);

        Console.WriteLine($"After 'S'. Sound write count: {machine.Bus.Sound.WriteCount}");

        Console.WriteLine("Pressing 1 for 1 player...");
        machine.Bus.Keyboard.SetKeyDown(0x30, true); // '1'
        machine.RunCycles(1_500_000);
        machine.Bus.Keyboard.SetKeyDown(0x30, false);

        Console.WriteLine($"After '1'. Sound write count: {machine.Bus.Sound.WriteCount}");

        Console.WriteLine("Running 5,000,000 cycles in gameplay...");
        // Press jump and movement keys to trigger sound effects
        machine.Bus.Keyboard.SetKeyDown(0x62, true); // Space (jump)
        machine.RunCycles(500_000);
        machine.Bus.Keyboard.SetKeyDown(0x62, false);
        machine.RunCycles(2_000_000);

        Console.WriteLine($"After gameplay. Sound write count: {machine.Bus.Sound.WriteCount}");
        for (int ch = 0; ch < 3; ch++)
        {
            Console.WriteLine($"  Tone {ch}: Period={machine.Bus.Sound.TonePeriod(ch)}, Vol={machine.Bus.Sound.Attenuation(ch)}");
        }
        Console.WriteLine($"  Noise: Ctrl={machine.Bus.Sound.NoiseControl}, Vol={machine.Bus.Sound.Attenuation(3)}");

        short[] pcm = machine.Bus.Sound.DrainSamples();
        Console.WriteLine($"Drained {pcm.Length} samples.");
        int nonZero = 0;
        short min = 0, max = 0;
        for (int i = 0; i < pcm.Length; i++)
        {
            if (pcm[i] != 0) nonZero++;
            if (pcm[i] < min) min = pcm[i];
            if (pcm[i] > max) max = pcm[i];
        }
        Console.WriteLine($"Non-zero samples: {nonZero} (Min: {min}, Max: {max})");
    }
}
