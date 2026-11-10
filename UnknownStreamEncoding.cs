using GotaSoundIO.IO;
using GotaSoundIO.Sound.Encoding;
using System;
using System.Collections.Generic;

namespace NitroFileLoader {

    /// <summary>
    /// Stream data of a format id this tool does not decode, kept as stored so the stream still lists, exports as STRM and saves unchanged.
    /// </summary>
    public class UnknownStreamEncoding : IAudioEncoding {

        /// <summary>
        /// Why anything beyond keeping the data fails.
        /// </summary>
        public const string Unsupported = "The stream's encoding is unknown, so it can only be kept or exported as STRM.";

        /// <summary>
        /// Block data as stored.
        /// </summary>
        private byte[] Data = new byte[0];

        /// <summary>
        /// Sample count the block table gives this block.
        /// </summary>
        private int Samples;

        /// <summary>
        /// Number of samples contained.
        /// </summary>
        /// <returns>Number of samples.</returns>
        public int SampleCount() => Samples;

        /// <summary>
        /// Data size contained.
        /// </summary>
        /// <returns>Data size.</returns>
        public int DataSize() => Data.Length;

        /// <summary>
        /// Get the number of samples from a block size, which an unknown encoding cannot tell.
        /// </summary>
        /// <param name="blockSize">Block size to get the number of samples from.</param>
        /// <returns>Number of samples.</returns>
        public int SamplesFromBlockSize(int blockSize) => throw new NotSupportedException(Unsupported);

        /// <summary>
        /// Raw data.
        /// </summary>
        /// <returns>Raw data.</returns>
        public object RawData() => Data;

        /// <summary>
        /// Read the raw data.
        /// </summary>
        /// <param name="r">File reader.</param>
        /// <param name="numSamples">Number of samples.</param>
        /// <param name="dataSize">Data size.</param>
        public void ReadRaw(FileReader r, uint numSamples, uint dataSize) {
            Data = r.ReadBytes((int)dataSize);
            Samples = (int)numSamples;
        }

        /// <summary>
        /// Write the raw data.
        /// </summary>
        /// <param name="w">File writer.</param>
        public void WriteRaw(FileWriter w) {
            w.Write(Data);
        }

        /// <summary>
        /// Encoding to an unknown format is not possible.
        /// </summary>
        public void FromFloatPCM(float[] pcm, object encodingData = null, int loopStart = -1, int loopEnd = -1) => throw new NotSupportedException(Unsupported);

        /// <summary>
        /// Decoding an unknown format is not possible.
        /// </summary>
        public float[] ToFloatPCM(object decodingData = null) => throw new NotSupportedException(Unsupported);

        /// <summary>
        /// Trimming needs the encoding.
        /// </summary>
        public void Trim(int totalSamples) => throw new NotSupportedException(Unsupported);

        /// <summary>
        /// Reblocking needs the encoding.
        /// </summary>
        public List<IAudioEncoding> ChangeBlockSize(List<IAudioEncoding> blocks, int newBlockSize) => throw new NotSupportedException(Unsupported);

        /// <summary>
        /// Get a property. There are none.
        /// </summary>
        public T GetProperty<T>(string propertyName) => default;

        /// <summary>
        /// Set a property. There are none.
        /// </summary>
        public void SetProperty<T>(T value, string propertyName) {}

        /// <summary>
        /// Duplicate the audio data.
        /// </summary>
        /// <returns>A copy of the audio data.</returns>
        public IAudioEncoding Duplicate() => new UnknownStreamEncoding() { Data = (byte[])Data.Clone(), Samples = Samples };

    }

}
