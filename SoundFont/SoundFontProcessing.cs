using GotaSoundBank.SF2;
using GotaSoundIO.IO;
using GotaSoundIO.Sound;
using GotaSoundIO.Sound.Encoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NitroFileLoader {

    /// <summary>
    /// Sample post-processing for exported SoundFonts: zero-order-hold upsampling and requantization.
    /// </summary>
    public static class SoundFontProcessing {

        /// <summary>
        /// Apply the sample processing requested by the options to every sample of the SoundFont.
        /// </summary>
        public static void Apply(SoundFont soundFont, SF2ExportOptions options) {
            if (options == null) {
                return;
            }
            foreach (var sample in soundFont.Samples) {
                if (options.Resample && sample.Wave.SampleRate < options.TargetSampleRate) {
                    sample.Wave = ResampleZeroOrderHold(sample.Wave, options.TargetSampleRate);
                }
                if (options.Quantize && options.BitDepth < 16) {
                    Quantize(sample.Wave, options.BitDepth);
                }
            }
        }

        /// <summary>
        /// Upsample a mono wave with zero-order hold (each source sample is repeated).
        /// Loop points are moved to the first output sample produced by the same source sample, so the loop
        /// still covers exactly the same source samples and the seam is unchanged.
        /// </summary>
        public static RiffWave ResampleZeroOrderHold(RiffWave wave, uint targetRate) {
            uint sourceRate = wave.SampleRate;
            if (sourceRate == 0 || targetRate <= sourceRate) {
                return wave;
            }
            short[] source = GetSamples(wave);
            long n = source.Length;
            int newLength = (int)CeilScale(n, targetRate, sourceRate);
            short[] output = new short[newLength];
            for (int j = 0; j < newLength; j++) {
                long i = (long)j * sourceRate / targetRate;
                if (i >= n) { i = n - 1; }
                output[j] = source[i];
            }
            RiffWave ret = MakeWave(output, targetRate);
            if (wave.Loops) {
                ret.Loops = true;
                ret.LoopStart = (uint)CeilScale(wave.LoopStart, targetRate, sourceRate);
                ret.LoopEnd = (uint)CeilScale(wave.LoopEnd, targetRate, sourceRate);
                if (ret.LoopEnd > newLength) { ret.LoopEnd = (uint)newLength; }
                if (ret.LoopStart > ret.LoopEnd) { ret.LoopStart = ret.LoopEnd; }
            }
            return ret;
        }

        /// <summary>
        /// Requantize 16-bit sample data to the given bit depth (mid-tread, rounded to nearest, silence stays 0).
        /// </summary>
        public static void Quantize(RiffWave wave, int bits) {
            if (bits < 1) { bits = 1; }
            if (bits >= 16) { return; }
            short[] data = GetSamples(wave);
            int step = 1 << (16 - bits);
            for (int i = 0; i < data.Length; i++) {
                long q = (long)Math.Round(data[i] / (double)step, MidpointRounding.AwayFromZero) * step;
                if (q > short.MaxValue) { q = short.MaxValue; }
                if (q < short.MinValue) { q = short.MinValue; }
                data[i] = (short)q;
            }
            ReplaceSamples(wave, data);
        }

        /// <summary>
        /// ceil(value * num / den) in integer arithmetic.
        /// </summary>
        private static long CeilScale(long value, long num, long den) {
            return (value * num + den - 1) / den;
        }

        /// <summary>
        /// Get the first channel of a wave as a flat 16-bit PCM array, converting the encoding if needed.
        /// </summary>
        public static short[] GetSamples(RiffWave wave) {
            if (wave.Audio.Channels.Count == 0) {
                return new short[0];
            }
            if (wave.Audio.EncodingType != typeof(PCM16)) {
                wave.Audio.Convert(typeof(PCM16), -1, wave.Loops ? (int)wave.LoopStart : -1, wave.Loops ? (int)wave.LoopEnd : -1);
            }
            var blocks = wave.Audio.Channels[0];
            if (blocks.Count == 1) {
                return (short[])blocks[0].RawData();
            }
            var all = new List<short>();
            foreach (var b in blocks) {
                all.AddRange((short[])b.RawData());
            }
            return all.ToArray();
        }

        /// <summary>
        /// Replace the audio of a wave with a single mono 16-bit PCM block.
        /// </summary>
        private static void ReplaceSamples(RiffWave wave, short[] data) {
            wave.Audio = new AudioData() {
                Channels = new List<List<IAudioEncoding>>() { new List<IAudioEncoding>() { Pcm16FromSamples(data) } }
            };
        }

        /// <summary>
        /// Build a new mono wave.
        /// </summary>
        private static RiffWave MakeWave(short[] data, uint sampleRate) {
            var wave = new RiffWave() { SampleRate = sampleRate };
            ReplaceSamples(wave, data);
            return wave;
        }

        /// <summary>
        /// Create a PCM16 block from raw samples without going through floating point.
        /// </summary>
        private static PCM16 Pcm16FromSamples(short[] data) {
            var pcm = new PCM16();
            using (var ms = new MemoryStream()) {
                var w = new FileWriter(ms);
                w.Write(data);
                w.Flush();
                ms.Position = 0;
                var r = new FileReader(ms);
                pcm.ReadRaw(r, (uint)data.Length, (uint)(data.Length * 2));
            }
            return pcm;
        }

    }

}
