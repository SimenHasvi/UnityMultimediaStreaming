namespace UnityMultimediaStreaming.Scripts.Audio
{
    /// <summary>
    /// Abstract class for an audio codec, providing methods for compression.
    /// </summary>
    public abstract class AudioCodec
    {
        /// <summary>
        /// The format of the audio we compress.
        /// </summary>
        public AudioFormat AudioFormat { get; }
        
        /// <summary>
        /// The desired bitrate from the encoder
        /// </summary>
        public int Bitrate { get; }
        
        /// <summary>
        /// The complexity used in the compression algorithms
        /// </summary>
        public int Complexity { get; }
        
        /// <summary>
        /// Constructor of the codec object.
        /// </summary>
        /// <param name="audioFormat">The format we compress.</param>
        /// <param name="bitrate">The desired bitrate of the compression.</param>
        protected AudioCodec(AudioFormat audioFormat, int bitrate, int complexity = 0)
        {
            AudioFormat = audioFormat;
            Bitrate = bitrate;
            Complexity = complexity;
            VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Created " + this);
        }

        /// <summary>
        /// Encode a single audio frame.
        /// </summary>
        /// <param name="frame">The frame to encode.</param>
        /// <param name="id">The id of the users which the frame came from, used for stateful encoders.</param>
        /// <returns>The encoded frame frame.</returns>
        public abstract byte[] Encode(short[] frame, int id = 0);

        /// <summary>
        /// Encode a single audio frame.
        /// </summary>
        /// <param name="frame">The frame to encode.</param>
        /// <param name="compressedFrame">The buffer containing the compressed frame on completion.</param>
        /// <param name="id">The id of the users which the frame came from, used for stateful encoders.</param>
        /// <returns>Length of the compressed data.</returns>
        public abstract int Encode(short[] frame, byte[] compressedFrame, int id = 0);

        /// <summary>
        /// Decode a given encoded frame.
        /// </summary>
        /// <param name="compressedFrame"></param>
        /// <param name="id">The id of the users which the frame came from, used for stateful encoders.</param>
        /// <returns>The decoded frame.</returns>
        public abstract short[] Decode(byte[] compressedFrame, int id = 0);

        /// <summary>
        /// Decode a given encoded frame.
        /// </summary>
        /// <param name="compressedFrame">The frame to decode</param>
        /// <param name="frame">Contains the decoded frame on completion.</param>
        /// <param name="id">The id of the users which the frame came from, used for stateful encoders.</param>
        public abstract void Decode(byte[] compressedFrame, short[] frame, int id = 0);

        /// <summary>
        /// Reset the state of the encoder.
        /// </summary>
        /// <param name="id">The specific encoder to reset.</param>
        public abstract void ResetEncoder(int id);

        /// <summary>
        /// Reset the state of the decoder.
        /// </summary>
        /// <param name="id">The specific decoder to reset.</param>
        public abstract void ResetDecoder(int id);
        
        /// <summary>
        /// Reset the state of the all the codecs.
        /// </summary>
        public abstract void Reset();

        public override string ToString()
        {
            return base.ToString() + "[bitrate:" + Bitrate + ", complexity:" + Complexity + ", format: " + AudioFormat + "]";
        }
    }
}