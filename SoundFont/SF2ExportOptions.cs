using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NitroFileLoader {

    /// <summary>
    /// Options for exporting an SBNK as a SoundFont 2 file.
    /// </summary>
    public class SF2ExportOptions {

        /// <summary>
        /// Upsample every sample below TargetSampleRate to TargetSampleRate using zero-order hold.
        /// </summary>
        public bool Resample = false;

        /// <summary>
        /// Target sample rate in Hz.
        /// </summary>
        public uint TargetSampleRate = 48000;

        /// <summary>
        /// Requantize sample data to BitDepth bits (still stored as 16-bit PCM).
        /// </summary>
        public bool Quantize = false;

        /// <summary>
        /// Target bit depth (1-16).
        /// </summary>
        public int BitDepth = 10;

        /// <summary>
        /// Scale the bundled PSG duty cycle and noise waves up to full scale.
        ///
        /// They are recorded about 15 dB below the level the DS sound driver actually outputs, so without this
        /// PSG and noise instruments are much quieter than the PCM instruments of the same bank.
        /// </summary>
        public bool BoostHardwareSamples = true;

        /// <summary>
        /// Name presets and instruments from InstrumentNames.
        /// </summary>
        public bool EmbedInstrumentNames = false;

        /// <summary>
        /// Path of the text file the names were loaded from (informational).
        /// </summary>
        public string InstrumentNamesPath = "";

        /// <summary>
        /// Instrument names keyed by SBNK instrument index.
        /// </summary>
        public Dictionary<int, string> InstrumentNames = new Dictionary<int, string>();

        /// <summary>
        /// Maximum length of an SF2 name field.
        /// </summary>
        public const int MaxNameLength = 20;

        /// <summary>
        /// Get the name for an instrument index, or null when none is set.
        /// </summary>
        public string GetInstrumentName(int index) {
            if (!EmbedInstrumentNames || InstrumentNames == null) {
                return null;
            }
            string name;
            return InstrumentNames.TryGetValue(index, out name) ? name : null;
        }

        /// <summary>
        /// Load instrument names from a text file into InstrumentNames.
        /// </summary>
        public void LoadInstrumentNames(string path) {
            InstrumentNamesPath = path;
            InstrumentNames = ReadInstrumentNames(path);
        }

        /// <summary>
        /// Read instrument names from a text file.
        ///
        /// One instrument per line. Fields are separated by ':', ',', ';', '=' or a tab.
        /// Accepted layouts:
        ///   index, name
        ///   index, bank, name        (instrument index = bank * 128 + index when bank is numeric)
        ///   index, anything, name    (SF2-Bank-Editor layout; the middle field is ignored if not numeric)
        /// Lines starting with '#' or '//' are ignored. A quoted file path as name is reduced to its file name.
        /// Names longer than 20 characters are shortened.
        /// </summary>
        public static Dictionary<int, string> ReadInstrumentNames(string path) {
            var names = new Dictionary<int, string>();
            char[] separators = new char[] { ':', ',', ';', '=', '\t' };
            foreach (string raw in File.ReadAllLines(path)) {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) {
                    continue;
                }
                string[] parts = line.Split(separators, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
                if (parts.Length < 2) {
                    continue;
                }
                int index;
                if (!int.TryParse(parts[0], out index)) {
                    continue;
                }
                string namePart;
                if (parts.Length >= 3) {
                    int bank;
                    if (int.TryParse(parts[1], out bank) && bank >= 0 && index < 128) {
                        index = bank * 128 + index;
                    }
                    namePart = string.Join(" ", parts.Skip(2));
                } else {
                    namePart = parts[1];
                }
                string name = CleanName(namePart);
                if (name.Length > 0) {
                    names[index] = name;
                }
            }
            return names;
        }

        /// <summary>
        /// Turn a raw name field into a valid SF2 name.
        /// </summary>
        public static string CleanName(string namePart) {
            string name = namePart.Trim();

            //Quoted path: keep the file name only.
            if (name.StartsWith("\"")) {
                name = name.Replace("\"", "");
                string[] subparts = name.Split(new char[] { '/', '\\' });
                name = Path.GetFileNameWithoutExtension(subparts[subparts.Length - 1]);
            }
            name = name.Replace('_', ' ').Trim();

            //SF2 name fields are ASCII.
            var sb = new StringBuilder();
            foreach (char c in name) {
                sb.Append(c >= 0x20 && c < 0x7F ? c : '?');
            }
            name = sb.ToString();

            if (name.Length > MaxNameLength) {
                name = ShortenName(name, MaxNameLength);
            }
            return name;
        }

        /// <summary>
        /// Shorten a name to a maximum length.
        /// </summary>
        public static string ShortenName(string name, int max) {
            name = name.Trim();
            while (name.Contains("  ")) {
                name = name.Replace("  ", " ");
            }
            if (name.Length <= max) {
                return name;
            }
            return name.Substring(0, max).TrimEnd();
        }

        /// <summary>
        /// Human readable description of the active options.
        /// </summary>
        public string Describe() {
            var parts = new List<string>();
            if (BoostHardwareSamples) { parts.Add("PSG and noise waves boosted to full scale"); }
            if (Resample) { parts.Add("resampled to " + TargetSampleRate + " Hz (zero-order hold)"); }
            if (Quantize) { parts.Add("quantized to " + BitDepth + " bits"); }
            if (EmbedInstrumentNames) { parts.Add("instrument names from " + Path.GetFileName(InstrumentNamesPath)); }
            return parts.Count == 0 ? "no sample processing" : string.Join(", ", parts);
        }

    }

}
