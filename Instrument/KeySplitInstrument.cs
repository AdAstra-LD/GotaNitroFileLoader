using System;
using System.Collections.Generic;
using System.Linq;
using GotaSequenceLib;
using GotaSoundIO.IO;

namespace NitroFileLoader {

    /// <summary>
    /// A key split instrument.
    /// </summary>
    public class KeySplitInstrument : Instrument {

        /// <summary>
        /// Read the instrument.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {

            //Get indices. End keys ascend, so only the first can be 0 (a region for note 0 alone); the list ends after the last non-zero key.
            byte[] keys = r.ReadBytes(8);
            int count = 1;
            for (int i = 1; i < 8; i++) {
                if (keys[i] != 0) {
                    count = i + 1;
                }
            }
            List<byte> indices = keys.Take(count).ToList();

            //Read note parameters.
            for (int i = 0; i < indices.Count; i++) {

                //Get the instrument type.
                InstrumentType t = (InstrumentType)r.ReadUInt16();
                NoteInfo.Add(r.Read<NoteInfo>());
                NoteInfo.Last().Key = (Notes)indices[i];
                NoteInfo.Last().InstrumentType = t;

            }

        }      

        /// <summary>
        /// Write the intrument.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {

            //Get indices.
            if (NoteInfo.Count < 1 || NoteInfo.Count > MaxInstruments()) {
                throw new InvalidOperationException($"Key split instrument {Index} has {NoteInfo.Count} regions; a key split holds 1 to {MaxInstruments()}.");
            }
            var indices = NoteInfo.Select(x => (byte)x.Key).ToArray();
            w.Write(indices);
            w.Write(new byte[8 - indices.Length]);

            //Write instrument info.
            foreach (var v in NoteInfo) {
                w.Write((ushort)v.InstrumentType);
                w.Write(v);
            }

        }

        /// <summary>
        /// The instrument type.
        /// </summary>
        /// <returns>The instrument type.</returns>
        public override InstrumentType Type() => InstrumentType.KeySplit;

        /// <summary>
        /// Max instruments.
        /// </summary>
        /// <returns>The max instruments.</returns>
        public override uint MaxInstruments() => 8;

    }
}
