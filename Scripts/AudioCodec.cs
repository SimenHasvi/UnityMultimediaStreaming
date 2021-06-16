namespace VoiceChat
{
    /// <summary>
    /// Abstract class for an audio codec, providing methods for compression.
    /// </summary>
    public abstract class AudioCodec
    {
        /// <summary>
        /// The format of the audio we compress.
        /// </summary>
        protected AudioFormat AudioFormat;
        
        /// <summary>
        /// Constructor of the codec object.
        /// </summary>
        /// <param name="audioFormat">The format we compress.</param>
        /// <param name="bitrate">The desired bitrate of the compression.</param>
        protected AudioCodec(AudioFormat audioFormat, int bitrate, int complexity)
        {
            AudioFormat = audioFormat;
        }

        /// <summary>
        /// Encode a single audio frame.
        /// </summary>
        /// <param name="frame">The frame to encode.</param>
        /// <returns>The encoded frame frame.</returns>
        public abstract byte[] Encode(short[] frame);

        /// <summary>
        /// Decode a given encoded frame.
        /// </summary>
        /// <param name="compressedFrame"></param>
        /// <returns>The decoded frame.</returns>
        public abstract short[] Decode(byte[] compressedFrame);
    }
}