using System;

namespace NitroFileLoader {

    /// <summary>
    /// Conversion between SBNK envelope parameters and SoundFont 2 generator values.
    ///
    /// The DS sound driver keeps the envelope level in units of 1/128 of 0.1 dB (1280 units per dB).
    /// Full level is 0 and the floor is -92544 (-72.3 dB). Attack multiplies the (negative) level by a
    /// per-tick coefficient; decay and release subtract a fixed rate per tick, so both are linear in dB,
    /// exactly like the SoundFont 2 decay/release phases. One envelope tick is 1/192 s (5.2 ms).
    ///
    /// The rate and level tables match the DS sound driver (see GotaSequenceLib.Playback.Utils); the attack
    /// times are the measured time-to-peak values for each attack setting.
    /// </summary>
    public static class DSEnvelope {

        /// <summary>
        /// Envelope units per decibel.
        /// </summary>
        public const double UnitsPerDecibel = 1280.0;

        /// <summary>
        /// Envelope floor in driver units (-72.3 dB).
        /// </summary>
        public const int LevelMin = -92544;

        /// <summary>
        /// Envelope tick length in milliseconds (192 Hz).
        /// </summary>
        public const double TickMilliseconds = 5.2;

        /// <summary>
        /// Decibel range covered by the SoundFont 2 decay and release generators.
        /// </summary>
        public const double SF2RampDecibels = 100.0;

        /// <summary>
        /// Smallest SoundFont 2 envelope time (1 ms).
        /// </summary>
        public const short MinTimecents = -12000;

        /// <summary>
        /// Largest SoundFont 2 envelope time (about 101.6 s).
        /// </summary>
        public const short MaxTimecents = 8000;

        /// <summary>
        /// Attack value to time-to-peak in milliseconds.
        /// </summary>
        public static readonly double[] AttackMilliseconds = new double[128] {
            8606.1, 4756.3, 3339.3, 2594.4, 2130.7, 1807.7, 1573.3, 1401.4,
            1255.5, 1140.9, 1047.1, 963.8, 896.0, 838.7, 786.6, 745.0,
            703.3, 666.8, 630.4, 599.1, 578.3, 547.0, 526.2, 505.3,
            484.5, 468.9, 448.0, 437.6, 416.8, 406.4, 395.9, 385.5,
            369.9, 359.5, 349.0, 338.6, 328.2, 323.0, 312.6, 307.4,
            297.0, 291.7, 286.5, 276.1, 270.9, 265.7, 260.5, 255.3,
            250.1, 244.9, 239.6, 234.4, 229.2, 224.0, 218.8, 213.6,
            213.6, 208.4, 203.2, 203.2, 198.0, 198.0, 192.8, 192.8,
            182.3, 182.3, 177.1, 177.1, 171.9, 171.9, 166.7, 166.7,
            161.5, 161.5, 156.3, 156.3, 151.1, 151.1, 145.9, 145.9,
            145.9, 145.9, 140.7, 140.7, 140.7, 130.2, 130.2, 130.2,
            125.0, 125.0, 125.0, 125.0, 119.8, 119.8, 119.8, 114.6,
            114.6, 114.6, 114.6, 109.4, 109.4, 109.4, 109.4, 109.4,
            104.2, 104.2, 104.2, 104.2, 99.0, 93.8, 88.6, 83.4,
            78.2, 72.9, 67.7, 62.5, 57.3, 52.1, 46.9, 41.7,
            36.5, 31.3, 26.1, 20.8, 15.6, 10.4, 10.4, 0.0
        };

        /// <summary>
        /// Decay/release value to driver rate (envelope units subtracted per tick).
        /// Dividing by UnitsPerDecibel * TickMilliseconds gives the decay/release speed in dB/ms.
        /// </summary>
        public static readonly ushort[] DecayRate = new ushort[128] {
            1, 3, 5, 7, 9, 11, 13, 15,
            17, 19, 21, 23, 25, 27, 29, 31,
            33, 35, 37, 39, 41, 43, 45, 47,
            49, 51, 53, 55, 57, 59, 61, 63,
            65, 67, 69, 71, 73, 75, 77, 79,
            81, 83, 85, 87, 89, 91, 93, 95,
            97, 99, 101, 102, 104, 105, 107, 108,
            110, 111, 113, 115, 116, 118, 120, 122,
            124, 126, 128, 130, 132, 135, 137, 140,
            142, 145, 148, 151, 154, 157, 160, 163,
            167, 171, 175, 179, 183, 187, 192, 197,
            202, 208, 213, 219, 226, 233, 240, 248,
            256, 265, 274, 284, 295, 307, 320, 334,
            349, 366, 384, 404, 427, 452, 480, 512,
            549, 591, 640, 698, 768, 853, 960, 1097,
            1280, 1536, 1920, 2560, 3840, 7680, 15360, 65535
        };

        /// <summary>
        /// Sustain value to envelope level in driver units (0 = full level, -92544 = silence).
        /// Every entry is a multiple of 128, so level / 128 is an exact centibel attenuation.
        /// </summary>
        public static readonly int[] SustainLevel = new int[128] {
            -92544, -92416, -92288, -83328, -76928, -71936, -67840, -64384,
            -61440, -58880, -56576, -54400, -52480, -50688, -49024, -47488,
            -46080, -44672, -43392, -42240, -41088, -40064, -39040, -38016,
            -36992, -36096, -35328, -34432, -33664, -32896, -32128, -31360,
            -30592, -29952, -29312, -28672, -28032, -27392, -26880, -26240,
            -25728, -25088, -24576, -24064, -23552, -23040, -22528, -22144,
            -21632, -21120, -20736, -20224, -19840, -19456, -19072, -18560,
            -18176, -17792, -17408, -17024, -16640, -16256, -16000, -15616,
            -15232, -14848, -14592, -14208, -13952, -13568, -13184, -12928,
            -12672, -12288, -12032, -11648, -11392, -11136, -10880, -10496,
            -10240, -9984, -9728, -9472, -9216, -8960, -8704, -8448,
            -8192, -7936, -7680, -7424, -7168, -6912, -6656, -6400,
            -6272, -6016, -5760, -5504, -5376, -5120, -4864, -4608,
            -4480, -4224, -3968, -3840, -3584, -3456, -3200, -2944,
            -2816, -2560, -2432, -2176, -2048, -1792, -1664, -1408,
            -1280, -1024, -896, -768, -512, -384, -128, 0
        };

        /// <summary>
        /// Decay or release speed in dB per millisecond.
        /// </summary>
        public static double DecaySpeedDbPerMs(byte value) {
            return DecayRate[Math.Min((int)value, 127)] / UnitsPerDecibel / TickMilliseconds;
        }

        /// <summary>
        /// Milliseconds a decay or release value needs to ramp the given number of decibels.
        /// With 72.3 dB this is the maximum release time on the DS.
        /// </summary>
        public static double DecayMilliseconds(byte value, double decibels) {
            return decibels / DecaySpeedDbPerMs(value);
        }

        /// <summary>
        /// Sustain level in decibels (0 = full level).
        /// </summary>
        public static double SustainDecibels(byte sustain) {
            return SustainLevel[Math.Min((int)sustain, 127)] / UnitsPerDecibel;
        }

        /// <summary>
        /// Convert milliseconds to SoundFont 2 absolute timecents, clamped to the generator range.
        /// </summary>
        public static short MillisecondsToTimecents(double milliseconds) {
            if (milliseconds <= 0) {
                return MinTimecents;
            }
            double tc = 1200 * Math.Log(milliseconds / 1000.0, 2);
            if (tc < MinTimecents) { return MinTimecents; }
            if (tc > MaxTimecents) { return MaxTimecents; }
            return (short)Math.Round(tc, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// SoundFont 2 attackVolEnv for an SBNK attack value (time to reach peak level).
        /// </summary>
        public static short AttackTimecents(byte attack) {
            return MillisecondsToTimecents(AttackMilliseconds[Math.Min((int)attack, 127)]);
        }

        /// <summary>
        /// SoundFont 2 decayVolEnv for an SBNK decay value.
        /// SF2 expresses decay as the time it would take to ramp 100 dB; the sustain level cuts it short,
        /// which is the same behaviour as the DS driver.
        /// </summary>
        public static short DecayTimecents(byte decay) {
            return MillisecondsToTimecents(DecayMilliseconds(decay, SF2RampDecibels));
        }

        /// <summary>
        /// SoundFont 2 releaseVolEnv for an SBNK release value (time to ramp 100 dB).
        /// </summary>
        public static short ReleaseTimecents(byte release) {
            return MillisecondsToTimecents(DecayMilliseconds(release, SF2RampDecibels));
        }

        /// <summary>
        /// SoundFont 2 sustainVolEnv (attenuation in centibels) for an SBNK sustain value.
        /// Sustain 0 is silence on the DS, so it maps to the maximum SF2 attenuation.
        /// </summary>
        public static short SustainCentibels(byte sustain) {
            if (sustain == 0) {
                return 1440;
            }
            return (short)Math.Round(-SustainLevel[Math.Min((int)sustain, 127)] / 128.0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// SoundFont 2 pan generator (-500 = left, 0 = center, 500 = right) for an SBNK pan (0..127, 64 = center).
        /// </summary>
        public static short PanToSF2(byte pan) {
            if (pan > 127) { pan = 127; }
            double value = pan <= 64 ? (pan - 64) * 500.0 / 64.0 : (pan - 64) * 500.0 / 63.0;
            return (short)Math.Round(value, MidpointRounding.AwayFromZero);
        }

    }

}
