using System;
using System.IO;

namespace BbcMicro.Core
{
    public class FloppyDrive
    {
        public const int BytesPerSector = 256;
        public const int SectorsPerTrack = 10;
        public const int TracksPerSide = 80;
        public const int DiskSize = TracksPerSide * SectorsPerTrack * BytesPerSector; // 204,800 bytes

        private byte[]? _diskData;
        public bool IsLoaded => _diskData != null;
        public int CurrentTrack { get; set; } = 0;
        public bool WriteProtected { get; set; } = false;

        public void LoadImage(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _diskData = null;
                return;
            }

            int targetSize = Math.Max(DiskSize, imageBytes.Length);
            _diskData = new byte[targetSize];
            Buffer.BlockCopy(imageBytes, 0, _diskData, 0, imageBytes.Length);
            CurrentTrack = 0;
        }

        public void LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Disk image not found: {path}");
            }
            byte[] bytes = File.ReadAllBytes(path);
            LoadImage(bytes);
        }

        public bool ReadSector(int track, int sector, byte[] destination, int destOffset)
        {
            if (_diskData == null) return false;
            if (track < 0 || track >= TracksPerSide) return false;
            if (sector < 0 || sector >= SectorsPerTrack) return false;

            int fileOffset = (track * SectorsPerTrack + sector) * BytesPerSector;
            if (fileOffset + BytesPerSector > _diskData.Length) return false;

            Buffer.BlockCopy(_diskData, fileOffset, destination, destOffset, BytesPerSector);
            return true;
        }

        public bool WriteSector(int track, int sector, byte[] source, int srcOffset)
        {
            if (_diskData == null || WriteProtected) return false;
            if (track < 0 || track >= TracksPerSide) return false;
            if (sector < 0 || sector >= SectorsPerTrack) return false;

            int fileOffset = (track * SectorsPerTrack + sector) * BytesPerSector;
            if (fileOffset + BytesPerSector > _diskData.Length) return false;

            Buffer.BlockCopy(source, srcOffset, _diskData, fileOffset, BytesPerSector);
            return true;
        }
    }
}
