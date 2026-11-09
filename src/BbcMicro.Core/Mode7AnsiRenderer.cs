using System;
using System.Text;

namespace BbcMicro.Core
{
    /// <summary>
    /// ANSI projection of actual BBC Mode 7 screen RAM. Colour control bytes
    /// occupy a character cell exactly as they do on the real teletext display.
    /// This renderer never synthesises the Matrix effect.
    /// </summary>
    public static class Mode7AnsiRenderer
    {
        public static string Render(byte[] ram, ushort screenBase = 0x7C00)
        {
            var sb = new StringBuilder(2200);
            for (int y = 0; y < 25; y++)
            {
                int previousColour = -1;
                int colour = 97; // Teletext resets to white at each scan line.
                int rowStart = screenBase + y * 40;

                for (int x = 0; x < 40; x++)
                {
                    int address = rowStart + x;
                    byte b = (byte)(address < ram.Length ? ram[address] & 0x7F : 32);

                    if (b >= 1 && b <= 7)
                    {
                        colour = b switch
                        {
                            1 => 91, 2 => 32, 3 => 93, 4 => 94,
                            5 => 95, 6 => 96, _ => 97
                        };
                    }

                    if (colour != previousColour)
                    {
                        sb.Append("\u001b[").Append(colour).Append('m');
                        previousColour = colour;
                    }

                    // Teletext control codes take space, rather than displaying glyphs.
                    sb.Append(b >= 32 && b <= 126 ? (char)b : ' ');
                }
                sb.Append("\u001b[0m\n");
            }
            return sb.ToString();
        }
    }
}
