using System;

namespace UnityMultimediaStreaming.Scripts.Audio
{
    
    /// <summary>
    /// Very simple networking module that will just write to its own local buffer.
    /// Good for testing stuff.
    /// </summary>
    public class LocalVoiceChatNetworkModule : VoiceChatNetworkModule
    {
        public LocalVoiceChatNetworkModule(AudioFormat audioFormat, AudioCodec audioCodec, int id = 0, string serverUri = "") : base(id, serverUri, audioFormat, audioCodec){}

        public override void StartListenForFrames(AudioFrameBuffer audioFrameBuffer)
        {
            AudioFrameBuffer = audioFrameBuffer;
        }

        public override void StopListenForFrames() {}

        public override void SendFrame(short[] frame, bool newCodecState = false)
        {
            //encode and decode before putting it in the buffer for debugging
            
            if (newCodecState) AudioCodec.Reset();
            var encodedFrame = new byte[AudioFormat.SamplesPerFrame];
            if (AudioCodec.GetType() == typeof(DummyAudioCodec)) encodedFrame = new byte[AudioFormat.SamplesPerFrame * sizeof(short)];
            var decodedFrame = new short[AudioFormat.SamplesPerFrame];
            var len = AudioCodec.Encode(frame, encodedFrame);
            Array.Resize(ref encodedFrame, len);
            AudioCodec.Decode(encodedFrame, decodedFrame);
            
            //var decodedFrame = new short[AudioFormat.SamplesPerFrame];
            //Array.Copy(frame, decodedFrame, frame.Length);
            AudioFrameBuffer.AddFrameToBuffer(decodedFrame, Id);
        }
    }
}