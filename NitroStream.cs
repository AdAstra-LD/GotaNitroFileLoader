using GotaSoundIO.IO;
using GotaSoundIO.Sound;
using GotaSoundIO.Sound.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NitroFileLoader {

    /// <summary>
    /// Stream.
    /// </summary>
    public class Stream : SoundFile {

        /// <summary>
        /// Supported encodings.
        /// </summary>
        /// <returns>The supported encodings.</returns>
        public override Type[] SupportedEncodings() => new Type[] { typeof(ImaAdpcm), typeof(PCM16), typeof(PCM8Signed) };

        /// <summary>
        /// Name.
        /// </summary>
        /// <returns>The name.</returns>
        public override string Name() => "STRM";

        /// <summary>
        /// Extensions.
        /// </summary>
        /// <returns>The extensions.</returns>
        public override string[] Extensions() => new string[] { "STRM" };

        /// <summary>
        /// Description.
        /// </summary>
        /// <returns>The description.</returns>
        public override string Description() => "An STRM stream used in Nintendo DS games.";

        /// <summary>
        /// If the file supports tracks.
        /// </summary>
        /// <returns>It doesn't.</returns>
        public override bool SupportsTracks() => false;

        /// <summary>
        /// Preferred encoding.
        /// </summary>
        /// <returns>The preferred encoding.</returns>
        public override Type PreferredEncoding() => typeof(ImaAdpcm);

        /// <summary>
        /// Playback timer as read, written back while the sample rate is still the one read with it.
        /// </summary>
        private ushort Timer;

        /// <summary>
        /// Sample rate read along with the timer.
        /// </summary>
        private uint TimerSampleRate;

        /// <summary>
        /// Header byte 0x1B, which no runtime reads.
        /// </summary>
        private byte HeaderPadding;

        /// <summary>
        /// The 32 reserved bytes closing the HEAD block.
        /// </summary>
        private byte[] Reserved = new byte[0x20];

        /// <summary>
        /// If the DATA block size counts only the audio bytes and the declared file size ends with them. The file itself is still padded to 4 bytes.
        /// </summary>
        private bool UnpaddedDataSize;

        /// <summary>
        /// Read the file.
        /// </summary>
        /// <param name="r">The reader.</param>
        public override void Read(FileReader r) {

            //Read header.
            r.OpenFile<NHeader>(out _);

            //Head block.
            r.OpenBlock(0, out _, out _);
            PcmFormat pcmFormat = (PcmFormat)r.ReadByte();
            Loops = r.ReadBoolean();
            int numChannels = r.ReadByte();
            HeaderPadding = r.ReadByte();
            SampleRate = TimerSampleRate = r.ReadUInt16();
            Timer = r.ReadUInt16();
            LoopStart = r.ReadUInt32();
            uint numSamples = r.ReadUInt32();
            LoopEnd = Loops ? numSamples : 0;
            r.OpenOffset("dataOffset");
            uint numBlocks = r.ReadUInt32();
            uint blockSize = r.ReadUInt32();
            uint blockSamples = r.ReadUInt32();
            uint lastBlockSize = r.ReadUInt32();
            uint lastBlockSamples = r.ReadUInt32();
            Reserved = r.ReadBytes(0x20);

            //Encoding type.
            Type encodingType = null;
            switch (pcmFormat) {
                case PcmFormat.SignedPCM8:
                    encodingType = typeof(PCM8Signed);
                    break;
                case PcmFormat.PCM16:
                    encodingType = typeof(PCM16);
                    break;
                case PcmFormat.Encoded:
                    encodingType = typeof(ImaAdpcm);
                    break;
            }

            //Data block.
            r.JumpToOffset("dataOffset", true, true);
            r.Position -= 4;
            long audioBytes = numBlocks == 0 ? 0 : (long)numChannels * ((long)(numBlocks - 1) * blockSize + lastBlockSize);
            UnpaddedDataSize = r.ReadUInt32() == audioBytes;
            try {
                Audio.Read(r, encodingType, numChannels, numBlocks, blockSize, blockSamples, lastBlockSize, lastBlockSamples, 0);
            } catch(Exception e){
                MessageBox.Show(e.ToString());
                return;
            }
        }

        /// <summary>
        /// Write the file.
        /// </summary>
        /// <param name="w">The writer.</param>
        public override void Write(FileWriter w) {

            //Init header.
            w.InitFile<NHeader>("STRM", ByteOrder.LittleEndian, null, 2);
            long countOff = w.Position - 2;

            //Head block.
            w.Write("HEAD".ToCharArray());
            w.Write((uint)0x50);

            //Format.
            if (Audio.EncodingType.Equals(typeof(PCM8Signed))) {
                w.Write((byte)PcmFormat.SignedPCM8);
            } else if (Audio.EncodingType.Equals(typeof(PCM16))) {
                w.Write((byte)PcmFormat.PCM16);
            } else if (Audio.EncodingType.Equals(typeof(ImaAdpcm))) {
                w.Write((byte)PcmFormat.Encoded);
            } else {
                throw new Exception("Invalid channel format!");
            }

            //Block table: blocked audio keeps its block size, unblocked audio is one block.
            uint blockSize = (uint)Audio.DataSize;
            uint blockSamples = (uint)Audio.NumSamples;
            if (Audio.BlockSize != -1) {
                blockSize = (uint)Audio.BlockSize;
                blockSamples = (uint)((IAudioEncoding)Activator.CreateInstance(Audio.EncodingType)).SamplesFromBlockSize(Audio.BlockSize);
            }

            //The timer read with the file is kept while its sample rate is.
            ushort timer = Timer != 0 && SampleRate == TimerSampleRate ? Timer : (ushort)Math.Floor((decimal)523655.96875 * ((decimal)1 / (decimal)SampleRate));
            w.Write(Loops);
            w.Write((byte)Audio.Channels.Count());
            w.Write(HeaderPadding);
            w.Write((ushort)SampleRate);
            w.Write(timer);
            w.Write(LoopStart);
            w.Write(Loops && LoopEnd != 0 ? LoopEnd : (uint)Audio.NumSamples);
            w.Write((uint)0x68);
            w.Write(Audio.NumBlocks);
            w.Write(blockSize);
            w.Write(blockSamples);
            w.Write(Audio.LastBlockSize);
            w.Write(Audio.LastBlockSamples);
            w.Write(Reserved != null && Reserved.Length == 0x20 ? Reserved : new byte[0x20]);
            long bak = w.Position;
            w.Write("DATA".ToCharArray());
            w.Write((uint)0);
            Audio.Write(w);
            long audioEnd = w.Position;
            if (!UnpaddedDataSize) {
                w.Pad(4);
            }
            long bak2 = w.Position;

            w.Position = bak + 4;
            w.Write((uint)(UnpaddedDataSize ? audioEnd - bak - 8 : bak2 - bak));
            w.Position = bak2;
            w.CloseFile();
            if (UnpaddedDataSize) {
                w.Pad(4);
            }
            bak = w.Position;

            w.Position = countOff;
            w.Write((ushort)2);
            w.Position = bak;
        }

        /// <summary>
        /// Before conversion.
        /// </summary>
        public override void BeforeConversion() {
            if (Audio.BlockSize == -1) {
                Audio.ChangeBlockSize(0x200);
            }
        }

        /// <summary>
        /// After conversion.
        /// </summary>
        public override void AfterConversion() {
            AlignLoopToBlock((uint)Audio.BlockSamples);
            TrimAfterLoopEnd();
            LoopEnd = (uint)Audio.NumSamples;
        }

    }

}
