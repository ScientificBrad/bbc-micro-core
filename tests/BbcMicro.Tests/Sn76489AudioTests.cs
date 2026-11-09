using System;
using System.Linq;
using BbcMicro.Core;
using Xunit;

namespace BbcMicro.Tests
{
    public class Sn76489AudioTests
    {
        [Fact]
        public void Test_SystemVia_IC32_LatchZero_TransfersPortAOnlyOnFallingEdge()
        {
            var via = new SystemVia6522(new KeyboardMatrix());
            var chip = new Sn76489();
            via.SoundWrite = chip.Write;
            via.Write(1, 0x90); // volume register for voice 0, loud
            via.Write(0, 0x08); // latch output zero high
            Assert.Equal(0L, chip.WriteCount);
            via.Write(0, 0x00); // selected bit zero falls: actual PSG write
            Assert.Equal(1L, chip.WriteCount);
            Assert.Equal(0, chip.Attenuation(0));
            via.Write(0, 0x00); // held-low does not repeatedly write
            Assert.Equal(1L, chip.WriteCount);
            via.Write(0, 0x08);
            via.Write(0, 0x00);
            Assert.Equal(2L, chip.WriteCount);
        }

        [Fact]
        public void Test_SN76489_ToneLatchAndDataBytes()
        {
            var chip = new Sn76489();
            chip.Write(0x85); // tone 0 lower nibble
            chip.Write(0x10); // tone 0 upper six bits
            Assert.Equal(261, chip.TonePeriod(0));
            chip.Write(0x9F); // voice 0 mute
            Assert.Equal(15, chip.Attenuation(0));
            chip.Write(0x90); // loud
            Assert.Equal(0, chip.Attenuation(0));
        }

        [Fact]
        public void Test_SN76489_ClockProduces882SamplesAt50Hz_AndSilenceOnMute()
        {
            var chip = new Sn76489();
            chip.Tick(40_000);
            short[] silent = chip.DrainSamples();
            Assert.Equal(882, silent.Length);
            Assert.All(silent, sample => Assert.Equal((short)0, sample));

            chip.Write(0x85);
            chip.Write(0x10); // period 261
            chip.Write(0x90); // loud voice 0
            chip.Tick(40_000);
            short[] audible = chip.DrainSamples();
            Assert.Equal(882, audible.Length);
            Assert.Contains(audible, sample => sample != 0);
            Assert.True(audible.Distinct().Count() >= 2);

            chip.Write(0x9F); // mute voice
            chip.Tick(40_000);
            Assert.All(chip.DrainSamples(), sample => Assert.Equal((short)0, sample));
        }

        [Fact]
        public void Test_SN76489_NoiseRegistersAndReset()
        {
            var chip = new Sn76489();
            chip.Write(0xE7); // white noise, tone-3-derived period
            chip.Write(0xF0); // noise channel loud
            Assert.Equal(7, chip.NoiseControl);
            Assert.Equal(0, chip.Attenuation(3));
            chip.Tick(40_000);
            Assert.Contains(chip.DrainSamples(), sample => sample != 0);
            chip.Reset();
            Assert.Equal(15, chip.Attenuation(3));
            Assert.Equal(0, chip.NoiseControl);
        }
    }
}
