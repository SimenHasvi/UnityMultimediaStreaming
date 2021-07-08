namespace UnityMultimediaStreaming.Scripts.Video
{
    public class LocalVideoStreamNetworkModule : VideoStreamNetworkModule
    {
        public LocalVideoStreamNetworkModule(int id = 0, string serverUri = "") : base(id, serverUri)
        {
        }

        public override void StartListenForFrames(VideoFrameBuffer videoFrameBuffer)
        {
            VideoFrameBuffer = videoFrameBuffer;
        }

        public override void StopListenForFrames()
        {
        }

        public override void SendFrame(byte[] frame)
        {
            VideoFrameBuffer.AddFrameToBuffer(frame, Id);
        }
    }
}