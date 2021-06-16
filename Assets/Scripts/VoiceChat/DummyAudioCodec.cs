namespace VoiceChat
{
    /// <summary>
    /// Audio codec object, that doesnt actually do anything.
    /// Used for debugging (or if you dont want to compress the audio for some reason).
    /// </summary>
    public class DummyAudioCodec : AudioCodec
    {
        public DummyAudioCodec(AudioFormat audioFormat, int bitrate = 0, int complexity = 0) : base(audioFormat, bitrate, complexity) {}

        public override byte[] Encode(short[] frame)
        {
            return VoiceChatUtils.ToByteStream(frame);
        }

        public override short[] Decode(byte[] compressedFrame)
        {
            return VoiceChatUtils.FromByteStream(compressedFrame);
        }
    }
}