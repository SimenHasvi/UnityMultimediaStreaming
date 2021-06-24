namespace VoiceChat
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

        public override void SendFrame(short[] frame)
        {
            //encode and decode before putting it in the buffer for debugging
            AudioFrameBuffer.AddFrameToBuffer(AudioCodec.Decode(AudioCodec.Encode(frame)), Id);
        }
    }
}