using System;
using System.IO;

namespace BbcMicro.Core
{
    public static class BmpWriter
    {
        public static void SaveBmp32(string path, int width, int height, uint[] bgraPixels)
        {
            if (width > 0 && bgraPixels.Length % width == 0)
            {
                height = bgraPixels.Length / width;
            }

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            int imageSize = width * height * 4;
            int fileSize = 54 + imageSize;

            // BITMAPFILEHEADER (14 bytes)
            bw.Write((byte)'B');
            bw.Write((byte)'M');
            bw.Write(fileSize);
            bw.Write((short)0); // Reserved 1
            bw.Write((short)0); // Reserved 2
            bw.Write(54);        // Offset to pixel data

            // BITMAPINFOHEADER (40 bytes)
            bw.Write(40);        // Header size
            bw.Write(width);
            bw.Write(-height);   // Negative for top-down bitmap
            bw.Write((short)1);  // Color planes
            bw.Write((short)32); // Bits per pixel
            bw.Write(0);         // BI_RGB (uncompressed)
            bw.Write(imageSize);
            bw.Write(2835);      // Pixels per meter horiz (72 DPI)
            bw.Write(2835);      // Pixels per meter vert
            bw.Write(0);         // Colors in color table
            bw.Write(0);         // Important colors

            // Pixel data (BGRA)
            for (int i = 0; i < bgraPixels.Length; i++)
            {
                bw.Write(bgraPixels[i]);
            }
        }
    }
}
