using System;
using System.IO;
using BbcMicro.Core;
using Xunit;

namespace BbcMicro.Tests
{
    public class RamVerificationTests
    {
        private static string FindFile(string relativePath)
        {
            string? dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }
            throw new FileNotFoundException($"Cannot find file: {relativePath}");
        }

        [Fact]
        public void Test_BootFile_LoadedIntoRam_MatchesDiskImage()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc999-chuckieEgg2023Web.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Issue *LOAD Boot 6000
            machine.TypeCommand("*LOAD Boot 6000");
            machine.RunCycles(4_000_000);

            byte[] expected = File.ReadAllBytes(FindFile("games/boot_dump.bin"));

            int diffCount = 0;
            int firstDiff = -1;
            for (int i = 0; i < expected.Length; i++)
            {
                byte actual = machine.Bus.Ram[0x6000 + i];
                if (actual != expected[i])
                {
                    diffCount++;
                    if (firstDiff == -1) firstDiff = i;
                }
            }

            if (diffCount > 0)
            {
                Assert.Fail($"Diff count: {diffCount}, first diff at offset 0x{firstDiff:X4} (addr 0x{(0x6000 + firstDiff):X4}): expected 0x{expected[firstDiff]:X2}, actual 0x{machine.Bus.Ram[0x6000 + firstDiff]:X2}");
            }
        }
    }
}
