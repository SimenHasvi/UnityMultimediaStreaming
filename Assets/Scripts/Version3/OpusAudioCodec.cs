using System;
using Concentus.Enums;
using Concentus.Structs;

namespace Version3
{
    public class OpusAudioCodec : AudioCodec
    {
        private readonly OpusEncoder _encoder;
        private readonly OpusDecoder _decoder;
        
        public OpusAudioCodec(AudioFormat audioFormat, int bitrate, int complexity) : base(audioFormat, bitrate, complexity)
        {
            _encoder = new OpusEncoder(AudioFormat.SamplingRate, AudioFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP)
            {
                Bitrate = bitrate,
                //UseVBR = true,
                //SignalType = OpusSignal.OPUS_SIGNAL_VOICE,
                //ForceMode = OpusMode.MODE_SILK_ONLY,
                Complexity = complexity
            };
            _decoder = new OpusDecoder(AudioFormat.SamplingRate, AudioFormat.Channels);
        }

        public override byte[] Encode(short[] frame)
        {
            var compressedFrame = new byte[frame.Length];
            var len = _encoder.Encode(frame, 0, frame.Length, compressedFrame, 0, frame.Length);
            Array.Resize(ref compressedFrame, len);
            return compressedFrame;
        }

        public override short[] Decode(byte[] compressedFrame)
        {
            var frame = new short[AudioFormat.SamplesPerFrame];
            _decoder.Decode(compressedFrame, 0, compressedFrame.Length, frame, 0, frame.Length);
            return frame;
        }
    }
}