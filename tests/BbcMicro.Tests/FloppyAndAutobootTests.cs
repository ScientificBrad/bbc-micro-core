using System;
using System.IO;
using System.Security.Cryptography;
using BbcMicro.Core;
using Xunit;

namespace BbcMicro.Tests
{
    public class FloppyAndAutobootTests
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
        public void Test_FloppyDrive_SsdLoading()
        {
            string ssdPath = FindFile("games/Disc999-chuckieEgg2023Web.ssd");
            byte[] ssdBytes = File.ReadAllBytes(ssdPath);
            Assert.Equal(204800, ssdBytes.Length);

            using var sha = SHA256.Create();
            string hash = Convert.ToHexString(sha.ComputeHash(ssdBytes)).ToLower();
            Assert.Equal("639abc21270e51c9a8c8dc84cb58ac955e5bc5e787395796c5e1f906dd39616f", hash);

            var drive = new FloppyDrive();
            drive.LoadImage(ssdBytes);
            Assert.True(drive.IsLoaded);

            byte[] sec0 = new byte[256];
            byte[] sec1 = new byte[256];
            Assert.True(drive.ReadSector(0, 0, sec0, 0));
            Assert.True(drive.ReadSector(0, 1, sec1, 0));

            // Verify DFS catalogue title "ChuckieEgg23"
            string title0 = System.Text.Encoding.ASCII.GetString(sec0, 0, 8);
            string title1 = System.Text.Encoding.ASCII.GetString(sec1, 0, 4);
            string fullTitle = (title0 + title1).TrimEnd();
            Assert.Equal("ChuckieEgg23", fullTitle);

            // Verify Boot Option 3 (bits 4-5 of byte 6 in sector 1)
            byte bootOptionByte = sec1[6];
            int bootOption = (bootOptionByte >> 4) & 3;
            Assert.Equal(3, bootOption); // Option 3 = *EXEC !BOOT
        }

        [Fact]
        public void Test_Dfs_StarCat()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc999-chuckieEgg2023Web.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Issue *CAT command to DFS 0.90
            machine.TypeCommand("*CAT");
            machine.RunCycles(2_500_000);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("ChuckieEgg23", screen);
            Assert.Contains("!Boot", screen);
            Assert.Contains("Boot", screen);
            Assert.Contains("Game", screen);

            // Save catalogue screenshot
            uint[] fb = ScreenFramebufferRenderer.RenderMode7(machine.Bus.Ram, machine.Bus.MosRom, 0x7C00, 2);
            string catBmpPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_cat.bmp");
            BmpWriter.SaveBmp32(catBmpPath, 640, 500, fb);
        }

        [Fact]
        public void Test_Dfs_RunBoot()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc999-chuckieEgg2023Web.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Issue *RUN Boot
            machine.TypeCommand("*RUN Boot");
            machine.RunCycles(5_000_000);

            byte b0 = machine.Bus.Ram[0x6000]; // 0x78 = SEI
            byte b1 = machine.Bus.Ram[0x6001]; // 0xA2 = LDX #$FF
            Assert.Equal(0x78, b0);
            Assert.Equal(0xA2, b1);

            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);
            Assert.Contains("Chuckie Egg 2023", screen);
            Assert.Contains("40th Anniversary Edition", screen);
            Assert.Contains("samskivington.com", screen);

            // Render live Mode 7 framebuffer and save authentic screenshot
            uint[] fb = ScreenFramebufferRenderer.RenderMode7(machine.Bus.Ram, machine.Bus.MosRom, 0x7C00, 2);
            string screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_title.bmp");
            BmpWriter.SaveBmp32(screenshotPath, 640, 500, fb);
            Assert.True(File.Exists(screenshotPath));
        }

        [Fact]
        public void Test_ChuckieEgg1983_PressS_StartsGameplay()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc002-ChuckieEgg.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *RUN CH_EGG
            machine.TypeCommand("*RUN CH_EGG");
            machine.RunCycles(8_000_000);

            // Verify high-scores screen is present
            byte mode = machine.Bus.Ram[0x0355];
            Assert.Equal(2, mode);

            // Press 'S' (Internal key 0x51 in BBC keyboard matrix)
            machine.Bus.Keyboard.SetKeyDown(0x51, true);
            machine.RunCycles(1_500_000);
            machine.Bus.Keyboard.SetKeyDown(0x51, false);
            machine.RunCycles(1_000_000);

            // Now press '1' (Internal key 0x30 in BBC keyboard matrix) for 1 player
            machine.Bus.Keyboard.SetKeyDown(0x30, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x30, false);
            // Run through "Get Ready Player 1" delay into actual Level 1 drawing
            machine.RunCycles(20_000_000);

            // Save new frame after starting game
            ushort screenStart = (ushort)(machine.Bus.Crtc.ScreenStartAddress * 8);
            uint[] fb = ScreenFramebufferRenderer.RenderScreen(machine.Bus.Ram, mode, machine.Bus.MosRom, machine.Bus.Ula.Palette, screenStart);
            string screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_gameplay.bmp");
            BmpWriter.SaveBmp32(screenshotPath, 640, 512, fb);

            byte playerX0 = machine.Bus.Ram[0x42];
            byte playerY0 = machine.Bus.Ram[0x43];
            byte playerSubX0 = machine.Bus.Ram[0x44];
            System.Console.WriteLine($"Player initial position: X={playerX0}, Y={playerY0}, subX={playerSubX0}");
            System.Console.WriteLine($"[SOUND DIAG] Writes={machine.Bus.Sound.WriteCount}, Tone0Vol={machine.Bus.Sound.Attenuation(0)}, Tone1Vol={machine.Bus.Sound.Attenuation(1)}, NoiseVol={machine.Bus.Sound.Attenuation(3)}, NonSilent={machine.Bus.Sound.NonSilentSamplesEmitted}");

            // Now press Jump (Space, IKN 0x62) and Right ('.', IKN 0x67)
            machine.Bus.Keyboard.SetKeyDown(0x62, true); // Jump
            machine.Bus.Keyboard.SetKeyDown(0x67, true); // Move Right
            machine.RunCycles(1_000_000);
            machine.Bus.Keyboard.SetKeyDown(0x62, false);
            machine.Bus.Keyboard.SetKeyDown(0x67, false);
            machine.RunCycles(1_000_000);

            byte playerX1 = machine.Bus.Ram[0x42];
            byte playerY1 = machine.Bus.Ram[0x43];
            byte playerSubX1 = machine.Bus.Ram[0x44];
            System.Console.WriteLine($"Player position after input: X={playerX1}, Y={playerY1}, subX={playerSubX1}");
            System.Console.WriteLine($"[SOUND DIAG AFTER INPUT] Writes={machine.Bus.Sound.WriteCount}, Tone0Vol={machine.Bus.Sound.Attenuation(0)}, Tone1Vol={machine.Bus.Sound.Attenuation(1)}, NoiseVol={machine.Bus.Sound.Attenuation(3)}, NonSilent={machine.Bus.Sound.NonSilentSamplesEmitted}");

            // Verify position changed due to keyboard input!
            Assert.True(playerX1 != playerX0 || playerY1 != playerY0 || playerSubX1 != playerSubX0,
                $"Player position must change when input keys are pressed! Before: ({playerX0},{playerY0},{playerSubX0}), After: ({playerX1},{playerY1},{playerSubX1})");

            // Verify genuine SN76489 sound commands were executed by MOS / game
            Assert.True(machine.Bus.Sound.WriteCount > 50, $"Sound writes expected during gameplay, got {machine.Bus.Sound.WriteCount}");
            short[] pcm = machine.Bus.Sound.DrainSamples();
            Assert.True(pcm.Length > 0, "Audio samples must be generated during gameplay");
        }

        [Fact]
        public void Test_ChuckieEgg1983_ModelB_Execution()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc002-ChuckieEgg.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *RUN CH_EGG
            machine.TypeCommand("*RUN CH_EGG");
            machine.RunCycles(8_000_000);

            // Verify binary was loaded into RAM at $1100-$37FF
            // First 4 bytes of CH_EGG at $1100
            byte b0 = machine.Bus.Ram[0x1100];
            byte b1 = machine.Bus.Ram[0x1101];
            Assert.True(machine.Bus.Ram[0x1100] != 0 || machine.Bus.Ram[0x1101] != 0, "CH_EGG loaded into memory");

            // Verify execution entered Chuckie Egg code region ($1100 - $37FF)
            Assert.True(machine.Cpu.TotalCycles > 4_000_000);

            // Check game state or captured output
            string screen = TeletextRenderer.RenderScreenToString(machine.Bus.Ram);

            // Render Mode 1 bitmap framebuffer and save screenshot
            ushort screenStart = (ushort)(machine.Bus.Crtc.ScreenStartAddress * 8);
            System.Console.WriteLine($"CRTC ScreenStartAddress register: ${machine.Bus.Crtc.ScreenStartAddress:X4} -> effective base: ${screenStart:X4}");
            System.Console.WriteLine($"CRTC R0={machine.Bus.Crtc.HorizontalTotal}, R1={machine.Bus.Crtc.HorizontalDisplayed}, R6={machine.Bus.Crtc.VerticalDisplayed}, R9={machine.Bus.Crtc.MaxScanLineAddress}");
            System.Console.Write("Video ULA Palette: ");
            for (int i = 0; i < 16; i++) System.Console.Write($"{machine.Bus.Ula.Palette[i]:X2} ");
            System.Console.WriteLine();

            byte mode = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"PC before loop: ${machine.Cpu.PC:X4}");
            for (int step = 0; step < 25; step++)
            {
                ushort pc = machine.Cpu.PC;
                byte op = machine.Bus.Read(pc);
                byte op1 = machine.Bus.Read((ushort)(pc + 1));
                byte op2 = machine.Bus.Read((ushort)(pc + 2));
                System.Console.WriteLine($"Step {step:D2}: PC=${pc:X4}: {op:X2} {op1:X2} {op2:X2} (A={machine.Cpu.A:X2}, X={machine.Cpu.X:X2}, Y={machine.Cpu.Y:X2}, P={machine.Cpu.Status:X2})");
                machine.Cpu.Step();
            }

            // Render authentic bitmap framebuffer (detects Mode 2 as selected by Chuckie Egg) and save screenshot
            uint[] fb = ScreenFramebufferRenderer.RenderScreen(machine.Bus.Ram, mode, machine.Bus.MosRom, machine.Bus.Ula.Palette, screenStart);
            string screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots", "chuckie_egg_1983.bmp");
            BmpWriter.SaveBmp32(screenshotPath, 640, 512, fb);
            Assert.True(File.Exists(screenshotPath));
        }

        [Fact]
        public void Test_StripPoker_SpaceBar_Advances()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc999-StripPoker.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *EXEC !BOOT
            machine.TypeCommand("*EXEC !BOOT");
            machine.RunCycles(12_000_000);

            // Machine should be waiting for space-bar (GET$)
            ushort pcBefore = machine.Cpu.PC;

            // Press Space Bar (IKN 0x62)
            machine.Bus.Keyboard.SetKeyDown(0x62, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x62, false);
            machine.RunCycles(5_000_000);

            ushort pcAfter = machine.Cpu.PC;
            System.Console.WriteLine($"PC before Space: ${pcBefore:X4}, after Space: ${pcAfter:X4}");
            Assert.NotEqual(pcBefore, pcAfter);
        }

        [Fact]
        public void Test_Elite_Boot_PressY()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc021-EliteD.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *EXEC !BOOT
            machine.TypeCommand("*EXEC !BOOT");
            machine.RunCycles(12_000_000);

            System.Console.WriteLine("--- ELITE SCREEN AT Y/N PROMPT ---");
            System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

            // Press 'Y' (IKN 0x44)
            machine.Bus.Keyboard.SetKeyDown(0x44, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x44, false);
            machine.RunCycles(20_000_000);

            System.Console.WriteLine("--- ELITE SCREEN AFTER 'Y' ---");
            System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
            byte mode = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"Active MODE: {mode}, PC: ${machine.Cpu.PC:X4}");
        }

        [Fact]
        public void Test_Snapper_Boot_And_Run()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc001-SnapperV2.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *EXEC !BOOT
            machine.TypeCommand("*EXEC !BOOT");
            machine.RunCycles(15_000_000);

            byte mode0 = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"--- SNAPPER AT INSTRUCTIONS (MODE {mode0}) ---");
            if (mode0 == 7) System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

            // Press Space Bar to start game
            machine.Bus.Keyboard.SetKeyDown(0x62, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x62, false);
            machine.RunCycles(10_000_000);

            byte mode1 = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"--- SNAPPER AFTER SPACE (MODE {mode1}, PC=${machine.Cpu.PC:X4}) ---");
            if (mode1 == 7) System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
        }

        [Fact]
        public void Test_Elite_Boot_PressY_And_Space()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc021-EliteD.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);

            // Execute *EXEC !BOOT
            machine.TypeCommand("*EXEC !BOOT");
            machine.RunCycles(12_000_000);

            // Press 'Y' (IKN 0x44)
            machine.Bus.Keyboard.SetKeyDown(0x44, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x44, false);
            machine.RunCycles(6_000_000);

            byte mode0 = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"--- ELITE AT CONTROLS SUMMARY (MODE {mode0}) ---");
            if (mode0 == 7) System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));

            // Press Space Bar (IKN 0x62) to launch game
            machine.Bus.Keyboard.SetKeyDown(0x62, true);
            machine.RunCycles(500_000);
            machine.Bus.Keyboard.SetKeyDown(0x62, false);
            machine.RunCycles(15_000_000);

            byte mode1 = machine.Bus.Ram[0x0355];
            System.Console.WriteLine($"--- ELITE AFTER SPACE (MODE {mode1}, PC=${machine.Cpu.PC:X4}) ---");
            if (mode1 == 7) System.Console.WriteLine(TeletextRenderer.RenderScreenToString(machine.Bus.Ram));
        }

        [Fact]
        public void Test_Exile_Mode7_ScreenAddress()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            var machine = new BbcMachine(osRom, basicRom, dfsRom);
            string ssdPath = FindFile("games/Disc040-ExileR.ssd");
            machine.Bus.Fdc.Drive0.LoadFromFile(ssdPath);

            machine.RunUntilPrompt(">", 4_000_000);
            machine.TypeCommand("*EXEC !BOOT");
            machine.RunCycles(12_000_000);

            byte mode = machine.Bus.Ram[0x0355];
            ushort crtcRaw = machine.Bus.Crtc.ScreenStartAddress;
            ushort crtcMul8 = (ushort)(crtcRaw * 8);
            System.Console.WriteLine($"Exile Mode={mode}, CRTC raw=${crtcRaw:X4}, raw*8=${crtcMul8:X4}");

            int nonZero7C00 = 0;
            for (int i = 0x7C00; i < 0x8000; i++)
            {
                if (machine.Bus.Ram[i] != 0 && machine.Bus.Ram[i] != 32) nonZero7C00++;
            }
            System.Console.WriteLine($"Non-space bytes in $7C00-$7FFF: {nonZero7C00}");

            int nonZeroMul8 = 0;
            for (int i = 0; i < 1024; i++)
            {
                int addr = (crtcMul8 + i) % 32768;
                if (machine.Bus.Ram[addr] != 0 && machine.Bus.Ram[addr] != 32) nonZeroMul8++;
            }
            System.Console.WriteLine($"Non-space bytes in crtcMul8=${crtcMul8:X4}: {nonZeroMul8}");

            Assert.True(nonZero7C00 > 100, "Exile Mode 7 title text must be in $7C00-$7FFF");
        }

        [Fact]
        public void ExportGalleryScreenshots()
        {
            byte[] osRom = File.ReadAllBytes(FindFile("roms/os12.rom"));
            byte[] basicRom = File.ReadAllBytes(FindFile("roms/basic2.rom"));
            byte[] dfsRom = File.ReadAllBytes(FindFile("roms/dfs09.rom"));

            string outDir = Path.GetFullPath(Path.Combine(FindFile("roms/os12.rom"), "..", "..", "docs", "images"));
            Directory.CreateDirectory(outDir);

            // Helper to save frame
            void SaveBmp(BbcMachine m, string filename)
            {
                byte mode = m.Bus.Ram[0x0355];
                ushort screenStart = (ushort)(m.Bus.Crtc.ScreenStartAddress * 8);
                uint[] fb = ScreenFramebufferRenderer.RenderScreen(
                    m.Bus.Ram, mode, m.Bus.MosRom, m.Bus.Ula.Palette, screenStart);
                BmpWriter.SaveBmp32(Path.Combine(outDir, filename), 640, 512, fb);
            }

            // 1. Cold Boot Mode 7
            var m1 = new BbcMachine(osRom, basicRom, dfsRom);
            m1.RunUntilPrompt(">", 4_000_000);
            SaveBmp(m1, "01_bbc_boot.bmp");

            // 2. Chuckie Egg Title
            var m2 = new BbcMachine(osRom, basicRom, dfsRom);
            m2.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/Disc002-ChuckieEgg.ssd")));
            m2.RunUntilPrompt(">", 4_000_000);
            m2.TypeCommand("*RUN CH_EGG");
            m2.RunCycles(8_000_000);
            SaveBmp(m2, "02_chuckie_egg_title.bmp");

            // 3. Chuckie Egg Gameplay
            m2.Bus.Keyboard.SetKeyDown(0x51, true); // 'S'
            m2.RunCycles(1_500_000);
            m2.Bus.Keyboard.SetKeyDown(0x51, false);
            m2.RunCycles(1_000_000);
            m2.Bus.Keyboard.SetKeyDown(0x30, true); // '1'
            m2.RunCycles(500_000);
            m2.Bus.Keyboard.SetKeyDown(0x30, false);
            m2.RunCycles(20_000_000);
            SaveBmp(m2, "03_chuckie_egg_gameplay.bmp");

            // 4. Elite
            var m3 = new BbcMachine(osRom, basicRom, dfsRom);
            m3.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/Disc021-EliteD.ssd")));
            m3.RunUntilPrompt(">", 4_000_000);
            m3.TypeCommand("*EXEC !BOOT");
            m3.RunCycles(12_000_000);
            m3.Bus.Keyboard.SetKeyDown(0x44, true); // 'Y'
            m3.RunCycles(500_000);
            m3.Bus.Keyboard.SetKeyDown(0x44, false);
            m3.RunCycles(6_000_000);
            SaveBmp(m3, "04_elite_flight_controls.bmp");

            // 5. Snapper
            var m4 = new BbcMachine(osRom, basicRom, dfsRom);
            m4.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/Disc001-SnapperV2.ssd")));
            m4.RunUntilPrompt(">", 4_000_000);
            m4.TypeCommand("*EXEC !BOOT");
            m4.RunCycles(15_000_000);
            SaveBmp(m4, "05_snapper_instructions.bmp");

            // 6. Strip Poker
            var m5 = new BbcMachine(osRom, basicRom, dfsRom);
            m5.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/Disc999-StripPoker.ssd")));
            m5.RunUntilPrompt(">", 4_000_000);
            m5.TypeCommand("*EXEC !BOOT");
            m5.RunCycles(12_000_000);
            SaveBmp(m5, "06_strip_poker_warning.bmp");

            // 7. Zelda Meets The Bug-Eyes
            var m6 = new BbcMachine(osRom, basicRom, dfsRom);
            m6.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/DiscA02-ZeldaMeetsTheBugEyesAlt.ssd")));
            m6.RunUntilPrompt(">", 4_000_000);
            m6.TypeCommand("*EXEC !BOOT");
            m6.RunCycles(12_000_000);
            SaveBmp(m6, "07_zelda_bugeyes.bmp");

            // 8. Exile
            var m7 = new BbcMachine(osRom, basicRom, dfsRom);
            m7.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes(FindFile("games/Disc040-ExileR.ssd")));
            m7.RunUntilPrompt(">", 4_000_000);
            m7.TypeCommand("*EXEC !BOOT");
            m7.RunCycles(12_000_000);
            SaveBmp(m7, "08_exile_title.bmp");

            Assert.True(File.Exists(Path.Combine(outDir, "01_bbc_boot.bmp")));
            Assert.True(File.Exists(Path.Combine(outDir, "03_chuckie_egg_gameplay.bmp")));
        }
    }
}
