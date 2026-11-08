using System;
using System.IO;
using System.Security.Cryptography;
using BbcMicro.Core;
using Xunit;

namespace BbcMicro.Tests
{
    public class FirmwareAndCpuTests
    {
        private static string FindRom(string filename)
        {
            string? dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, "roms", filename);
                if (File.Exists(candidate)) return candidate;

                string candidateRoot = Path.Combine(dir, filename);
                if (File.Exists(candidateRoot)) return candidateRoot;

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new FileNotFoundException($"Cannot find ROM: {filename} from {AppContext.BaseDirectory}");
        }

        private static string FindReferenceFile(string relativePath)
        {
            string? dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                string candidate = Path.Combine(dir, relativePath);
                if (File.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new FileNotFoundException($"Cannot find reference file: {relativePath}");
        }

        [Fact]
        public void Test_RomInventoryHashes()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            Assert.Equal(16384, osRom.Length);
            Assert.Equal(16384, basicRom.Length);
            Assert.Equal(8192, dfsRom.Length);

            using var md5 = MD5.Create();
            string osMd5 = Convert.ToHexString(md5.ComputeHash(osRom)).ToLower();
            string basicMd5 = Convert.ToHexString(md5.ComputeHash(basicRom)).ToLower();
            string dfsMd5 = Convert.ToHexString(md5.ComputeHash(dfsRom)).ToLower();

            Assert.Equal("3f724370ece4e4b790f26a8eca196e89", osMd5);
            Assert.Equal("2cc67be4624df4dc66617742571a8e3d", basicMd5);
            Assert.Equal("803530149c274994fabc6d28cbe03679", dfsMd5);
        }

        [Fact]
        public void Test_TobyNelsonReassemblyVerification()
        {
            string binPath = FindReferenceFile(Path.Combine("references", "tobylobster", "os120.bin"));
            byte[] data = File.ReadAllBytes(binPath);
            Assert.Equal(16384, data.Length);
            using var md5 = MD5.Create();
            string hash = Convert.ToHexString(md5.ComputeHash(data)).ToLower();
            Assert.Equal("0a59a5ba15fe8557b5f7fee32bbd393a", hash);
        }

        [Fact]
        public void Test_MosBootSequence()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.RunUntilPrompt(">", 4_000_000);

            string output = machine.CapturedOutput.ToString();
            Assert.Contains("BBC Computer", output);
            Assert.Contains("32K", output);
            Assert.Contains("Acorn DFS", output);
            Assert.Contains("BASIC", output);
            Assert.Contains(">", output);
            Assert.Equal(15, machine.Bus.SelectedSlot);
        }

        [Fact]
        public void Test_MemoryLayout_PageAndHimem()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.RunUntilPrompt(">", 4_000_000);

            machine.TypeCommand("PRINT ~PAGE, ~HIMEM");
            machine.RunCycles(1_500_000);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("1900", screen);
            Assert.Contains("7C00", screen);
        }

        [Fact]
        public void Test_BasicExecution_PrintMath()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.RunUntilPrompt(">", 4_000_000);

            machine.TypeCommand("PRINT 123 * 456");
            machine.RunCycles(1_500_000);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("56088", screen);
        }

        [Fact]
        public void Test_BasicExecution_ForLoop()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.RunUntilPrompt(">", 4_000_000);

            machine.TypeCommand("FOR I=1 TO 3:PRINT I*10:NEXT");
            machine.RunCycles(1_500_000);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("10", screen);
            Assert.Contains("20", screen);
            Assert.Contains("30", screen);
        }

        [Fact]
        public void Test_Cli_StarHelp()
        {
            byte[] osRom = File.ReadAllBytes(FindRom("os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindRom("basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindRom("dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            machine.RunUntilPrompt(">", 4_000_000);

            machine.TypeCommand("*HELP");
            machine.RunCycles(1_500_000);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("DFS 0.90", screen);
            Assert.Contains("OS 1.20", screen);
        }
    }
}
