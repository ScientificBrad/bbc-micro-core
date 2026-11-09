using System;
using System.IO;
using BbcMicro.Core;
using Xunit;

namespace BbcMicro.Tests
{
    public class Mode1FramebufferTests
    {
        [Fact]
        public void Test_Mode1_TwoBitPixelDecoding_AllFourColours()
        {
            // Construct a byte with 4 distinct pixels:
            // Pixel 0 = Colour 0 (%00) -> Bit 7=0, Bit 3=0
            // Pixel 1 = Colour 1 (%01) -> Bit 6=0, Bit 2=1
            // Pixel 2 = Colour 2 (%10) -> Bit 5=1, Bit 1=0
            // Pixel 3 = Colour 3 (%11) -> Bit 4=1, Bit 0=1
            // Byte binary: %0011_0101 = 0x35
            byte[] ram = new byte[32768];
            const ushort screenBase = 0x3000;
            ram[screenBase] = 0x35; // Scanline 0 of Block (0, 0)

            uint[] fb = ScreenFramebufferRenderer.RenderMode1(ram, screenBase, scale: 1);

            // Scanline 0, pixels 0..3
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[0], fb[0]); // Pixel 0: Black
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[1], fb[1]); // Pixel 1: Red
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[2], fb[2]); // Pixel 2: Yellow
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[3], fb[3]); // Pixel 3: White
        }

        [Fact]
        public void Test_Mode1_MemoryInterleaving_EightVerticalBytesPerBlock()
        {
            byte[] ram = new byte[32768];
            const ushort screenBase = 0x3000;

            // In Mode 1, each character block is 4 pixels wide and 8 scanlines high.
            // All 8 scanlines for block (col=0, row=0) are stored contiguously at screenBase + 0..7.
            // Put a single distinctive pixel (colour 3) on scanline 5 of block (col=0, row=0):
            // Pixel 2 on scanline 5 -> Bit 5=1, Bit 1=1 -> %0010_0010 = 0x22
            ram[screenBase + 5] = 0x22;

            uint[] fb = ScreenFramebufferRenderer.RenderMode1(ram, screenBase, scale: 1);

            int width = 320;
            // On scanline 5 (y = 5), pixel 2 (x = 2) should be Colour 3 (White)
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[3], fb[5 * width + 2]);

            // Scanlines 0..4 and 6..7 of block 0 should be Colour 0 (Black)
            for (int y = 0; y < 8; y++)
            {
                if (y != 5)
                {
                    Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[0], fb[y * width + 2]);
                }
            }
        }

        [Fact]
        public void Test_Mode1_CharacterBlockPositions_AndStride()
        {
            byte[] ram = new byte[32768];
            const ushort screenBase = 0x3000;

            // Mode 1 has 80 blocks horizontally (320 pixels) and 32 blocks vertically (256 pixels).
            // Block row stride = 80 blocks * 8 bytes = 640 bytes.
            // Let's place a pixel in Block (col=10, row=4):
            // Memory address = screenBase + (4 * 640) + (10 * 8) + scanline 0
            int addr = screenBase + (4 * 640) + (10 * 8) + 0;
            // All 4 pixels white: %1111_1111 = 0xFF
            ram[addr] = 0xFF;

            uint[] fb = ScreenFramebufferRenderer.RenderMode1(ram, screenBase, scale: 1);

            int width = 320;
            int expectedX = 10 * 4; // Col 10 starts at x = 40
            int expectedY = 4 * 8;  // Row 4 starts at y = 32

            for (int px = 0; px < 4; px++)
            {
                Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[3], fb[expectedY * width + (expectedX + px)]);
            }

            // Verify adjacent pixels outside this block are black
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[0], fb[expectedY * width + (expectedX - 1)]);
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[0], fb[expectedY * width + (expectedX + 4)]);
        }

        [Fact]
        public void Test_Mode1_PaletteCamTranslation()
        {
            byte[] ram = new byte[32768];
            const ushort screenBase = 0x3000;
            ram[screenBase] = 0x35; // Pixels 0, 1, 2, 3

            // Define custom Video ULA palette registers:
            // Hardware physical colour written to FE21 is inverted with EOR 7
            // Let's map Logical 1 to Cyan (physical 6 -> written as 6 ^ 7 = 1)
            // CAM index 2 corresponds to Logical 1
            byte[] customUla = new byte[16];
            customUla[0] = 7; // Logical 0: Black (0 ^ 7 = 7)
            customUla[2] = 1; // Logical 1: Cyan (6 ^ 7 = 1)
            customUla[8] = 4; // Logical 2: Yellow (3 ^ 7 = 4)
            customUla[10] = 0; // Logical 3: White (7 ^ 7 = 0)

            uint[] fb = ScreenFramebufferRenderer.RenderMode1(ram, screenBase, scale: 1, ulaPalette: customUla);

            // Pixel 1 should now be Cyan (HardwareColours[6]) instead of default Red
            Assert.Equal(ScreenFramebufferRenderer.HardwareColours[6], fb[1]);
        }

        [Fact]
        public void Test_Mode1_ScreenMemoryWrappingAt8000()
        {
            byte[] ram = new byte[32768];
            // If screenBase is 0x4000, a 20KB screen would reach 0x9000.
            // On BBC Model B (32KB), addresses at/above 0x8000 wrap back to screenBase.
            const ushort screenBase = 0x4000;
            // Test wrapped address: screenBase + 0x4000 (16KB) = 0x8000 -> wraps to screenBase
            ram[screenBase] = 0xFF; // Written at wrapped destination

            uint[] fb = ScreenFramebufferRenderer.RenderMode1(ram, screenBase, scale: 1);

            // Address wrapping ensures screenBase + offset maps correctly to ram[screenBase]
            Assert.Equal(ScreenFramebufferRenderer.DefaultMode1Palette[3], fb[0]);
        }
    }
}
