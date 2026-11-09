using System;

namespace BbcMicro.Core
{
    public static class ScreenFramebufferRenderer
    {
        public static uint Bgra(byte r, byte g, byte b, byte a = 255) =>
            (uint)(b | (g << 8) | (r << 16) | (a << 24));

        // BBC Micro physical hardware colours (0 to 7) in 32-bit BGRA format
        public static readonly uint[] HardwareColours = new uint[]
        {
            Bgra(0, 0, 0),       // 0: Black
            Bgra(255, 0, 0),     // 1: Red
            Bgra(0, 255, 0),     // 2: Green
            Bgra(255, 255, 0),   // 3: Yellow
            Bgra(0, 0, 255),     // 4: Blue
            Bgra(255, 0, 255),   // 5: Magenta
            Bgra(0, 255, 255),   // 6: Cyan
            Bgra(255, 255, 255)  // 7: White
        };

        // BBC Teletext 8 basic colours (BGRA format)
        private static readonly uint[] TeletextColours = HardwareColours;

        // Standard default Mode 1 palette (Logical 0..3 -> Physical 0, 1, 3, 7: Black, Red, Yellow, White)
        public static readonly uint[] DefaultMode1Palette = new uint[]
        {
            HardwareColours[0], // 0: Black
            HardwareColours[1], // 1: Red
            HardwareColours[3], // 2: Yellow
            HardwareColours[7]  // 3: White
        };

        // Standard default Mode 2 palette (Logical 0..15 -> Physical 0..7 repeated)
        public static readonly uint[] DefaultMode2Palette = new uint[]
        {
            HardwareColours[0], // 0: Black
            HardwareColours[1], // 1: Red
            HardwareColours[2], // 2: Green
            HardwareColours[3], // 3: Yellow
            HardwareColours[4], // 4: Blue
            HardwareColours[5], // 5: Magenta
            HardwareColours[6], // 6: Cyan
            HardwareColours[7], // 7: White
            HardwareColours[0], // 8: Black
            HardwareColours[1], // 9: Red
            HardwareColours[2], // 10: Green
            HardwareColours[3], // 11: Yellow
            HardwareColours[4], // 12: Blue
            HardwareColours[5], // 13: Magenta
            HardwareColours[6], // 14: Cyan
            HardwareColours[7]  // 15: White
        };

        public static uint[] RenderMode7(byte[] ram, byte[]? mosRom = null, ushort screenBase = 0x7C00, int scale = 2)
        {
            const int cols = 40;
            const int rows = 25;
            const int charW = 8;
            const int charH = 10; // 10 scanlines per character row in standard Mode 7

            int width = cols * charW * scale;
            int height = rows * charH * scale;
            uint[] framebuffer = new uint[width * height];

            for (int r = 0; r < rows; r++)
            {
                uint fgColour = TeletextColours[7]; // White
                uint bgColour = TeletextColours[0]; // Black
                bool graphicsMode = false;
                bool contiguousGraphics = true;
                int rowOffset = screenBase + (r * cols);

                for (int c = 0; c < cols; c++)
                {
                    int addr = rowOffset + c;
                    byte b = (addr < ram.Length) ? (byte)(ram[addr] & 0x7F) : (byte)0x20;

                    // Teletext control codes
                    if (b >= 1 && b <= 7)
                    {
                        fgColour = TeletextColours[b];
                        graphicsMode = false;
                    }
                    else if (b >= 17 && b <= 23)
                    {
                        fgColour = TeletextColours[b - 16];
                        graphicsMode = true;
                    }
                    else if (b == 28)
                    {
                        bgColour = TeletextColours[0]; // Black background
                    }
                    else if (b == 29)
                    {
                        bgColour = fgColour; // New background
                    }
                    else if (b == 25)
                    {
                        contiguousGraphics = true;
                    }
                    else if (b == 26)
                    {
                        contiguousGraphics = false;
                    }

                    // Render 8x10 character cell
                    int cellX = c * charW * scale;
                    int cellY = r * charH * scale;

                    if (b < 32)
                    {
                        // Control codes display as background spaces
                        FillRect(framebuffer, width, cellX, cellY, charW * scale, charH * scale, bgColour);
                    }
                    else if (graphicsMode && b >= 32)
                    {
                        // 2x3 block sixel graphics
                        RenderSixel(framebuffer, width, cellX, cellY, b, fgColour, bgColour, contiguousGraphics, scale);
                    }
                    else
                    {
                        // Alphanumeric character from MOS ROM font or fallback
                        RenderAlphanumeric(framebuffer, width, cellX, cellY, b, fgColour, bgColour, mosRom, scale);
                    }
                }
            }

            return framebuffer;
        }

        private static void FillRect(uint[] fb, int fbWidth, int x, int y, int w, int h, uint colour)
        {
            for (int dy = 0; dy < h; dy++)
            {
                int rowIdx = (y + dy) * fbWidth + x;
                for (int dx = 0; dx < w; dx++)
                {
                    fb[rowIdx + dx] = colour;
                }
            }
        }

        private static void RenderAlphanumeric(uint[] fb, int fbWidth, int x, int y, byte ch, uint fg, uint bg, byte[]? mosRom, int scale)
        {
            byte[] glyph = new byte[8];
            if (mosRom != null && mosRom.Length >= 0x0300 && ch >= 32 && ch < 128)
            {
                Buffer.BlockCopy(mosRom, (ch - 32) * 8, glyph, 0, 8);
            }
            else
            {
                // Blank fallback
                Array.Clear(glyph, 0, 8);
            }

            for (int py = 0; py < 10; py++)
            {
                byte rowBits = (py < 8) ? glyph[py] : (byte)0; // Scanlines 8-9 are blank spacing
                for (int px = 0; px < 8; px++)
                {
                    bool pixelOn = (rowBits & (1 << (7 - px))) != 0;
                    uint colour = pixelOn ? fg : bg;

                    for (int sy = 0; sy < scale; sy++)
                    {
                        int rowIdx = (y + py * scale + sy) * fbWidth + (x + px * scale);
                        for (int sx = 0; sx < scale; sx++)
                        {
                            fb[rowIdx + sx] = colour;
                        }
                    }
                }
            }
        }

        private static void RenderSixel(uint[] fb, int fbWidth, int x, int y, byte ch, uint fg, uint bg, bool contiguous, int scale)
        {
            // Sixel bit mapping:
            // Bit 0: Top left,     Bit 1: Top right
            // Bit 2: Middle left,  Bit 3: Middle right
            // Bit 4: Bottom left,  Bit 6: Bottom right (bit 5 is ASCII offset)
            int bits = ch - 32;
            bool tl = (bits & 0x01) != 0;
            bool tr = (bits & 0x02) != 0;
            bool ml = (bits & 0x04) != 0;
            bool mr = (bits & 0x08) != 0;
            bool bl = (bits & 0x10) != 0;
            bool br = (bits & 0x40) != 0;

            for (int py = 0; py < 10; py++)
            {
                int blockY = py < 3 ? 0 : (py < 7 ? 1 : 2);
                for (int px = 0; px < 8; px++)
                {
                    int blockX = px < 4 ? 0 : 1;
                    bool active = blockY switch
                    {
                        0 => blockX == 0 ? tl : tr,
                        1 => blockX == 0 ? ml : mr,
                        _ => blockX == 0 ? bl : br
                    };

                    uint colour = active ? fg : bg;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        int rowIdx = (y + py * scale + sy) * fbWidth + (x + px * scale);
                        for (int sx = 0; sx < scale; sx++)
                        {
                            fb[rowIdx + sx] = colour;
                        }
                    }
                }
            }
        }

        public static uint[] RenderMode1(byte[] ram, ushort screenBase = 0x3000, int scale = 2, byte[]? ulaPalette = null)
        {
            const int cols = 80; // 80 character blocks wide (each 4 pixels wide)
            const int rows = 32; // 32 character blocks high (each 8 scanlines high)
            const int blockW = 4;
            const int blockH = 8;

            int width = cols * blockW * scale;   // 80 * 4 * scale = 320 * scale = 640
            int height = rows * blockH * scale; // 32 * 8 * scale = 256 * scale = 512
            uint[] framebuffer = new uint[width * height];

            // Resolve palette (either from Video ULA CAM or default Mode 1 palette)
            uint[] palette = new uint[4];
            if (ulaPalette != null && ulaPalette.Length >= 16 && (ulaPalette[0] != 0 || ulaPalette[2] != 0 || ulaPalette[8] != 0 || ulaPalette[10] != 0))
            {
                // In Mode 1:
                // Logical 0: CAM index 0 (%0000)
                // Logical 1: CAM index 2 (%0010)
                // Logical 2: CAM index 8 (%1000)
                // Logical 3: CAM index 10 (%1010)
                // Physical colour = (val & 7) ^ 7
                palette[0] = HardwareColours[(ulaPalette[0] & 7) ^ 7];
                palette[1] = HardwareColours[(ulaPalette[2] & 7) ^ 7];
                palette[2] = HardwareColours[(ulaPalette[8] & 7) ^ 7];
                palette[3] = HardwareColours[(ulaPalette[10] & 7) ^ 7];
            }
            else
            {
                Array.Copy(DefaultMode1Palette, palette, 4);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int charBase = screenBase + (r * cols * 8) + (c * 8);

                    for (int py = 0; py < 8; py++)
                    {
                        int addr = charBase + py;
                        // Support 20KB screen memory wrapping if screen crosses $8000 boundary
                        if (addr >= 0x8000)
                        {
                            addr = screenBase + ((addr - 0x8000) % 20480);
                        }

                        byte val = addr < ram.Length ? ram[addr] : (byte)0;

                        // 4 pixels per byte in Mode 1:
                        // Pixel 0 (leftmost): Bit 7 (MSB), Bit 3 (LSB)
                        // Pixel 1: Bit 6 (MSB), Bit 2 (LSB)
                        // Pixel 2: Bit 5 (MSB), Bit 1 (LSB)
                        // Pixel 3 (rightmost): Bit 4 (MSB), Bit 0 (LSB)
                        int p0 = ((val >> 6) & 2) | ((val >> 3) & 1);
                        int p1 = ((val >> 5) & 2) | ((val >> 2) & 1);
                        int p2 = ((val >> 4) & 2) | ((val >> 1) & 1);
                        int p3 = ((val >> 3) & 2) | (val & 1);

                        int[] pixColours = { p0, p1, p2, p3 };

                        for (int px = 0; px < 4; px++)
                        {
                            uint col = palette[pixColours[px] & 3];
                            int screenX = (c * 4 + px) * scale;
                            int screenY = (r * 8 + py) * scale;

                            for (int sy = 0; sy < scale; sy++)
                            {
                                int rowIdx = (screenY + sy) * width + screenX;
                                for (int sx = 0; sx < scale; sx++)
                                {
                                    framebuffer[rowIdx + sx] = col;
                                }
                            }
                        }
                    }
                }
            }

            return framebuffer;
        }

        public static uint[] RenderMode2(byte[] ram, ushort screenBase = 0x3000, int scaleX = 4, int scaleY = 2, byte[]? ulaPalette = null)
        {
            const int cols = 80; // 80 character blocks wide (each 2 pixels wide)
            const int rows = 32; // 32 character blocks high (each 8 scanlines high)
            const int blockW = 2; // 2 pixels per byte in Mode 2
            const int blockH = 8; // 8 scanlines per block

            int width = cols * blockW * scaleX;   // 80 * 2 * 4 = 160 * 4 = 640
            int height = rows * blockH * scaleY; // 32 * 8 * 2 = 256 * 2 = 512
            uint[] framebuffer = new uint[width * height];

            // Resolve palette (either from Video ULA CAM or default Mode 2 palette)
            uint[] palette = new uint[16];
            if (ulaPalette != null && ulaPalette.Length >= 16)
            {
                for (int i = 0; i < 16; i++)
                {
                    palette[i] = HardwareColours[(ulaPalette[i] & 7) ^ 7];
                }
            }
            else
            {
                Array.Copy(DefaultMode2Palette, palette, 16);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int charBase = screenBase + (r * cols * 8) + (c * 8);

                    for (int py = 0; py < 8; py++)
                    {
                        int addr = charBase + py;
                        if (addr >= 0x8000)
                        {
                            addr = screenBase + ((addr - 0x8000) % 20480);
                        }

                        byte val = addr < ram.Length ? ram[addr] : (byte)0;

                        // 2 pixels per byte in Mode 2 (each pixel is 4 bits):
                        // Left Pixel: Bits 7, 5, 3, 1
                        // Right Pixel: Bits 6, 4, 2, 0
                        int pLeft = ((val >> 4) & 8) | ((val >> 3) & 4) | ((val >> 2) & 2) | ((val >> 1) & 1);
                        int pRight = ((val >> 3) & 8) | ((val >> 2) & 4) | ((val >> 1) & 2) | (val & 1);

                        int[] pixColours = { pLeft, pRight };

                        for (int px = 0; px < 2; px++)
                        {
                            uint col = palette[pixColours[px] & 15];
                            int screenX = (c * 2 + px) * scaleX;
                            int screenY = (r * 8 + py) * scaleY;

                            for (int sy = 0; sy < scaleY; sy++)
                            {
                                int rowIdx = (screenY + sy) * width + screenX;
                                for (int sx = 0; sx < scaleX; sx++)
                                {
                                    framebuffer[rowIdx + sx] = col;
                                }
                            }
                        }
                    }
                }
            }

            return framebuffer;
        }

        public static uint[] RenderMode5(byte[] ram, ushort screenBase = 0x5800, int scaleX = 4, int scaleY = 2, byte[]? ulaPalette = null)
        {
            const int cols = 40; // 40 character blocks wide (each 4 pixels wide)
            const int rows = 32; // 32 character blocks high (each 8 scanlines high)
            const int blockW = 4;
            const int blockH = 8;

            int width = cols * blockW * scaleX / 2; // 40 * 4 * 4 / 2 = 320 * 2 = 640
            int height = rows * blockH * scaleY;    // 32 * 8 * 2 = 512
            uint[] framebuffer = new uint[width * height];

            uint[] palette = new uint[4];
            if (ulaPalette != null && ulaPalette.Length >= 16 && (ulaPalette[0] != 0 || ulaPalette[2] != 0 || ulaPalette[8] != 0 || ulaPalette[10] != 0))
            {
                palette[0] = HardwareColours[(ulaPalette[0] & 7) ^ 7];
                palette[1] = HardwareColours[(ulaPalette[2] & 7) ^ 7];
                palette[2] = HardwareColours[(ulaPalette[8] & 7) ^ 7];
                palette[3] = HardwareColours[(ulaPalette[10] & 7) ^ 7];
            }
            else
            {
                Array.Copy(DefaultMode1Palette, palette, 4);
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int charBase = screenBase + (r * cols * 8) + (c * 8);

                    for (int py = 0; py < 8; py++)
                    {
                        int addr = charBase + py;
                        if (addr >= 0x8000)
                        {
                            addr = screenBase + ((addr - 0x8000) % 10240);
                        }

                        byte val = addr < ram.Length ? ram[addr] : (byte)0;

                        int p0 = ((val >> 6) & 2) | ((val >> 3) & 1);
                        int p1 = ((val >> 5) & 2) | ((val >> 2) & 1);
                        int p2 = ((val >> 4) & 2) | ((val >> 1) & 1);
                        int p3 = ((val >> 3) & 2) | (val & 1);

                        int screenY = (r * blockH + py) * scaleY;
                        int screenX = (c * blockW) * scaleX;

                        uint col0 = palette[p0 & 3];
                        uint col1 = palette[p1 & 3];
                        uint col2 = palette[p2 & 3];
                        uint col3 = palette[p3 & 3];

                        for (int sy = 0; sy < scaleY; sy++)
                        {
                            int yOffset = (screenY + sy) * width;
                            for (int sx = 0; sx < scaleX; sx++)
                            {
                                framebuffer[yOffset + screenX + sx] = col0;
                                framebuffer[yOffset + screenX + scaleX + sx] = col1;
                                framebuffer[yOffset + screenX + (scaleX * 2) + sx] = col2;
                                framebuffer[yOffset + screenX + (scaleX * 3) + sx] = col3;
                            }
                        }
                    }
                }
            }

            return framebuffer;
        }

        public static uint[] RenderScreen(byte[] ram, byte mode, byte[]? mosRom = null, byte[]? ulaPalette = null, ushort screenBase = 0x3000)
        {
            return mode switch
            {
                1 => RenderMode1(ram, screenBase < 0x3000 || screenBase >= 0x8000 ? (ushort)0x3000 : screenBase, 2, ulaPalette),
                2 => RenderMode2(ram, screenBase < 0x3000 || screenBase >= 0x8000 ? (ushort)0x3000 : screenBase, 4, 2, ulaPalette),
                5 => RenderMode5(ram, screenBase < 0x5800 || screenBase >= 0x8000 ? (ushort)0x5800 : screenBase, 4, 2, ulaPalette),
                7 => RenderMode7(ram, mosRom, (ushort)(0x7C00 + (screenBase % 1024)), 2),
                _ => RenderMode1(ram, screenBase < 0x3000 || screenBase >= 0x8000 ? (ushort)0x3000 : screenBase, 2, ulaPalette)
            };
        }
    }
}
