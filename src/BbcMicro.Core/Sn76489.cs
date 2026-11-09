using System;
using System.Collections.Generic;

namespace BbcMicro.Core
{
    /// <summary>
    /// BBC Model B SN76489AN: 4 MHz input, divided internally by 16.
    /// Driven from the host's 2 MHz clock (one PSG tick per 8 CPU cycles).
    /// Three programmable square-wave voices and one 15-bit noise LFSR.
    /// </summary>
    public sealed class Sn76489
    {
        public const int HostClockHz = 2_000_000;
        public const int SampleRate = 44_100;
        public const int PsgDividerInHostCycles = 8;

        private readonly int[] _period = { 1, 1, 1 };
        private readonly int[] _counter = { 1, 1, 1, 1 };
        private readonly bool[] _state = { true, true, true, true };
        private readonly int[] _volume = { 15, 15, 15, 15 };
        private static readonly double[] Amp = CreateAmplitudeTable();
        private readonly List<short> _pcm = new(1100);
        private int _latch;
        private int _noiseControl;
        private int _noiseShift = 0x4000;
        private int _psgCycleAccumulator;
        private int _sampleCycleAccumulator;
        public long WriteCount { get; private set; }
        public long NonSilentSamplesEmitted { get; private set; }

        private static double[] CreateAmplitudeTable()
        {
            double[] a = new double[16];
            for (int i = 0; i < 15; i++) a[i] = Math.Pow(10, -i / 10.0);
            a[15] = 0;
            return a;
        }

        public int TonePeriod(int channel) => _period[channel];
        public int Attenuation(int channel) => _volume[channel];
        public int NoiseControl => _noiseControl;

        public void Reset()
        {
            Array.Fill(_period, 1);
            Array.Fill(_counter, 1);
            Array.Fill(_state, true);
            Array.Fill(_volume, 15);
            _noiseControl = 0;
            _noiseShift = 0x4000;
            _latch = 0;
            _psgCycleAccumulator = 0;
            _sampleCycleAccumulator = 0;
            _pcm.Clear();
            WriteCount = 0;
            NonSilentSamplesEmitted = 0;
        }

        public void Write(byte data)
        {
            WriteCount++;
            if ((data & 0x80) != 0)
            {
                _latch = (data >> 4) & 7;
                int nibble = data & 15;
                if (_latch == 6) { _noiseControl = nibble & 7; _noiseShift = 0x4000; }
                else if ((_latch & 1) != 0) _volume[_latch >> 1] = nibble;
                else
                {
                    int channel = _latch >> 1;
                    _period[channel] = (_period[channel] & 0x3F0) | nibble;
                }
            }
            else
            {
                if (_latch == 6) { _noiseControl = data & 7; _noiseShift = 0x4000; }
                else if ((_latch & 1) != 0) _volume[_latch >> 1] = data & 15;
                else
                {
                    int channel = _latch >> 1;
                    _period[channel] = (_period[channel] & 15) | ((data & 63) << 4);
                }
            }
        }

        private void StepChip()
        {
            for (int i = 0; i < 3; i++)
            {
                if (--_counter[i] <= 0)
                {
                    _counter[i] = _period[i] == 0 ? 1024 : _period[i];
                    _state[i] = !_state[i];
                }
            }
            if (--_counter[3] <= 0)
            {
                int rate = _noiseControl & 3;
                _counter[3] = rate == 3 ? (_period[2] == 0 ? 1024 : _period[2])
                    : 16 << rate;
                _state[3] = !_state[3];
                if (_state[3])
                {
                    int feedback = (_noiseShift & 1);
                    if ((_noiseControl & 4) != 0) feedback ^= (_noiseShift >> 1) & 1;
                    _noiseShift = (_noiseShift >> 1) | (feedback << 14);
                }
            }
        }

        private short Sample()
        {
            double sum = 0, active = 0;
            for (int i = 0; i < 4; i++)
            {
                double amplitude = Amp[_volume[i]];
                active += amplitude;
                bool high = i == 3 ? (_noiseShift & 1) != 0 : _state[i];
                if (high) sum += amplitude;
            }
            // Center around zero; scale so up to 4 simultaneous channels fit cleanly in 16-bit range
            // while giving punchy, authentic volume for individual sound effects.
            double mixed = (sum - active * 0.5) * 0.5;
            return (short)Math.Clamp((int)Math.Round(mixed * 32767), -32768, 32767);
        }

        /// <summary>Advance chip/sample clocks using the cycles already executed by the 6502.</summary>
        public void Tick(int cpuCycles)
        {
            // Process source clocks in CPU-cycle order, preserving fractional
            // resampling so 40,000 CPU cycles produce exactly 882 PCM samples.
            for (int c = 0; c < cpuCycles; c++)
            {
                if (++_psgCycleAccumulator >= PsgDividerInHostCycles)
                {
                    _psgCycleAccumulator = 0;
                    StepChip();
                }
                _sampleCycleAccumulator += SampleRate;
                if (_sampleCycleAccumulator >= HostClockHz)
                {
                    _sampleCycleAccumulator -= HostClockHz;
                    short pcm = Sample();
                    if (pcm != 0) NonSilentSamplesEmitted++;
                    _pcm.Add(pcm);
                }
            }
        }

        public short[] DrainSamples()
        {
            short[] data = _pcm.ToArray();
            _pcm.Clear();
            return data;
        }

        public void DiscardSamples() => _pcm.Clear();
    }
}
