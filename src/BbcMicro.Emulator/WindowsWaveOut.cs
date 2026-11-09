using System;
using System.Runtime.InteropServices;

namespace BbcMicro.Emulator
{
    /// <summary>Native WinMM buffered 44.1 kHz mono PCM output.
    /// All calls except disposal are made by the emulation worker thread.</summary>
    internal sealed class WindowsWaveOut : IDisposable
    {
        private const int Slots = 16;
        private const int MaxSamples = 1100;
        private const uint WhdrDone = 1;
        private static readonly int HeaderSize = Marshal.SizeOf<WaveHeader>();

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WaveFormat
        {
            public ushort FormatTag;
            public ushort Channels;
            public uint SamplesPerSec;
            public uint AvgBytesPerSec;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public ushort ExtraSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr Data;
            public uint BufferLength;
            public uint BytesRecorded;
            public IntPtr User;
            public uint Flags;
            public uint Loops;
            public IntPtr Next;
            public IntPtr Reserved;
        }

        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutOpen(out IntPtr handle, uint deviceId, ref WaveFormat format,
            IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutPrepareHeader(IntPtr h, IntPtr header, uint size);
        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutUnprepareHeader(IntPtr h, IntPtr header, uint size);
        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutWrite(IntPtr h, IntPtr header, uint size);
        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutReset(IntPtr h);
        [DllImport("winmm.dll", ExactSpelling = true)]
        private static extern uint waveOutClose(IntPtr h);

        private readonly IntPtr[] _buffers = new IntPtr[Slots];
        private readonly IntPtr[] _headers = new IntPtr[Slots];
        private readonly bool[] _prepared = new bool[Slots];
        private IntPtr _handle;
        private int _next;
        private bool _disposed;
        public long Submitted { get; private set; }
        public long Dropped { get; private set; }

        public WindowsWaveOut()
        {
            var format = new WaveFormat
            {
                FormatTag = 1, Channels = 1, SamplesPerSec = 44100,
                AvgBytesPerSec = 88200, BlockAlign = 2, BitsPerSample = 16,
                ExtraSize = 0
            };
            uint error = waveOutOpen(out _handle, 0xFFFFFFFF, ref format, IntPtr.Zero, IntPtr.Zero, 0);
            if (error != 0) throw new InvalidOperationException(
                "Windows audio output unavailable (waveOutOpen error " + error + ")");
            try
            {
                for (int i = 0; i < Slots; i++)
                {
                    _buffers[i] = Marshal.AllocHGlobal(MaxSamples * 2);
                    _headers[i] = Marshal.AllocHGlobal(HeaderSize);
                    Marshal.StructureToPtr(new WaveHeader { Data = _buffers[i] }, _headers[i], false);
                }
            }
            catch { Dispose(); throw; }
        }

        public void Queue(short[] samples)
        {
            if (_disposed || samples.Length == 0) return;
            if (samples.Length > MaxSamples)
            {
                Dropped++;
                return;
            }

            int i = _next;
            if (_prepared[i])
            {
                var prior = Marshal.PtrToStructure<WaveHeader>(_headers[i]);
                if ((prior.Flags & WhdrDone) == 0)
                {
                    // Keep CPU/video on time instead of blocking a stalled sound device.
                    Dropped++;
                    return;
                }
                if (waveOutUnprepareHeader(_handle, _headers[i], (uint)HeaderSize) != 0)
                {
                    Dropped++;
                    return;
                }
                _prepared[i] = false;
            }

            Marshal.Copy(samples, 0, _buffers[i], samples.Length);
            Marshal.StructureToPtr(new WaveHeader
            {
                Data = _buffers[i],
                BufferLength = (uint)(samples.Length * 2)
            }, _headers[i], false);
            if (waveOutPrepareHeader(_handle, _headers[i], (uint)HeaderSize) != 0)
            {
                Dropped++;
                return;
            }
            _prepared[i] = true;
            if (waveOutWrite(_handle, _headers[i], (uint)HeaderSize) != 0)
            {
                Dropped++;
                return;
            }
            Submitted++;
            _next = (i + 1) % Slots;
        }

        public void Stop()
        {
            if (_disposed) return;
            waveOutReset(_handle);
            for (int i = 0; i < Slots; i++)
            {
                if (!_prepared[i]) continue;
                if (waveOutUnprepareHeader(_handle, _headers[i], (uint)HeaderSize) == 0)
                    _prepared[i] = false;
            }
            _next = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
            if (_handle != IntPtr.Zero) waveOutClose(_handle);
            for (int i = 0; i < Slots; i++)
            {
                if (_headers[i] != IntPtr.Zero) Marshal.FreeHGlobal(_headers[i]);
                if (_buffers[i] != IntPtr.Zero) Marshal.FreeHGlobal(_buffers[i]);
            }
        }
    }
}
