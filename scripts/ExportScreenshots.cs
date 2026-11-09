using System;
using System.IO;
using BbcMicro.Core;

namespace BbcMicro.Scripts
{
    public class ExportScreenshots
    {
        public static void Main()
        {
            byte[] osRom = File.ReadAllBytes("roms/os12.rom");
            byte[] basicRom = File.ReadAllBytes("roms/basic2.rom");
            byte[] dfsRom = File.ReadAllBytes("roms/dfs09.rom");

            Directory.CreateDirectory("docs/images");

            // 1. Cold Boot Mode 7
            Console.WriteLine("Capturing 01_bbc_boot...");
            var m1 = new BbcMachine(osRom, basicRom, dfsRom);
            m1.RunUntilPrompt(">", 4_000_000);
            SaveFrame(m1, "docs/images/01_bbc_boot.bmp");

            // 2. Chuckie Egg Title
            Console.WriteLine("Capturing 02_chuckie_egg_title...");
            var m2 = new BbcMachine(osRom, basicRom, dfsRom);
            m2.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc002-ChuckieEgg.ssd"));
            m2.RunUntilPrompt(">", 4_000_000);
            m2.TypeCommand("*RUN CH_EGG");
            m2.RunCycles(8_000_000);
            SaveFrame(m2, "docs/images/02_chuckie_egg_title.bmp");

            // 3. Chuckie Egg Gameplay
            Console.WriteLine("Capturing 03_chuckie_egg_gameplay...");
            m2.Bus.Keyboard.SetKeyDown(0x51, true); // 'S'
            m2.RunCycles(1_500_000);
            m2.Bus.Keyboard.SetKeyDown(0x51, false);
            m2.RunCycles(1_000_000);
            m2.Bus.Keyboard.SetKeyDown(0x30, true); // '1'
            m2.RunCycles(500_000);
            m2.Bus.Keyboard.SetKeyDown(0x30, false);
            m2.RunCycles(20_000_000);
            SaveFrame(m2, "docs/images/03_chuckie_egg_gameplay.bmp");

            // 4. Elite
            Console.WriteLine("Capturing 04_elite_flight_controls...");
            var m3 = new BbcMachine(osRom, basicRom, dfsRom);
            m3.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc021-EliteD.ssd"));
            m3.RunUntilPrompt(">", 4_000_000);
            m3.TypeCommand("*EXEC !BOOT");
            m3.RunCycles(12_000_000);
            m3.Bus.Keyboard.SetKeyDown(0x44, true); // 'Y'
            m3.RunCycles(500_000);
            m3.Bus.Keyboard.SetKeyDown(0x44, false);
            m3.RunCycles(6_000_000);
            SaveFrame(m3, "docs/images/04_elite_flight_controls.bmp");

            // 5. Snapper
            Console.WriteLine("Capturing 05_snapper_instructions...");
            var m4 = new BbcMachine(osRom, basicRom, dfsRom);
            m4.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc001-SnapperV2.ssd"));
            m4.RunUntilPrompt(">", 4_000_000);
            m4.TypeCommand("*EXEC !BOOT");
            m4.RunCycles(15_000_000);
            SaveFrame(m4, "docs/images/05_snapper_instructions.bmp");

            // 6. Strip Poker
            Console.WriteLine("Capturing 06_strip_poker_warning...");
            var m5 = new BbcMachine(osRom, basicRom, dfsRom);
            m5.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc999-StripPoker.ssd"));
            m5.RunUntilPrompt(">", 4_000_000);
            m5.TypeCommand("*EXEC !BOOT");
            m5.RunCycles(12_000_000);
            SaveFrame(m5, "docs/images/06_strip_poker_warning.bmp");

            // 7. Zelda Meets The Bug-Eyes
            Console.WriteLine("Capturing 07_zelda_bugeyes...");
            var m6 = new BbcMachine(osRom, basicRom, dfsRom);
            m6.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/DiscA02-ZeldaMeetsTheBugEyesAlt.ssd"));
            m6.RunUntilPrompt(">", 4_000_000);
            m6.TypeCommand("*EXEC !BOOT");
            m6.RunCycles(12_000_000);
            SaveFrame(m6, "docs/images/07_zelda_bugeyes.bmp");

            // 8. Exile
            Console.WriteLine("Capturing 08_exile_title...");
            var m7 = new BbcMachine(osRom, basicRom, dfsRom);
            m7.Bus.Fdc.Drive0.LoadImage(File.ReadAllBytes("games/Disc040-ExileR.ssd"));
            m7.RunUntilPrompt(">", 4_000_000);
            m7.TypeCommand("*EXEC !BOOT");
            m7.RunCycles(12_000_000);
            SaveFrame(m7, "docs/images/08_exile_title.bmp");

            Console.WriteLine("All gallery BMPs captured successfully.");
        }

        private static void SaveFrame(BbcMachine m, string path)
        {
            byte mode = m.Bus.Ram[0x0355];
            ushort screenStart = (ushort)(m.Bus.Crtc.ScreenStartAddress * 8);
            uint[] fb = ScreenFramebufferRenderer.RenderScreen(
                m.Bus.Ram, mode, m.Bus.MosRom, m.Bus.Ula.Palette, screenStart);
            BmpWriter.SaveBmp32(path, 640, 512, fb);
        }
    }
}
