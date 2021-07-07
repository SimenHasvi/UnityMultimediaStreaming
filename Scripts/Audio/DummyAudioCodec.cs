using System;
using System.Drawing;

namespace VoiceChat
{
    /// <summary>
    /// Audio codec object, that doesnt actually do anything.
    /// Used for debugging (or if you dont want to compress the audio for some reason).
    /// </summary>
    public class DummyAudioCodec : AudioCodec
    {
        public DummyAudioCodec(AudioFormat audioFormat, int bitrate = 0, int complexity = 0) : base(audioFormat, bitrate, complexity) {}

        public override byte[] Encode(short[] frame, int id = 0)
        {
            return VoiceChatUtils.ToByteStream(frame);
        }

        public override int Encode(short[] frame, byte[] compressedFrame, int id = 0)
        {
            VoiceChatUtils.ToByteStream(compressedFrame, frame);
            return compressedFrame.Length;
        }

        public override short[] Decode(byte[] compressedFrame, int id = 0)
        {
            return VoiceChatUtils.FromByteStream(compressedFrame);
        }

        public override void Decode(byte[] compressedFrame, short[] frame, int id = 0)
        {
            VoiceChatUtils.FromByteStream(frame, compressedFrame);
        }

        public override void ResetEncoder(int id)
        {
        }

        public override void ResetDecoder(int id)
        {
        }

        public override void Reset()
        {
        }
    }
}