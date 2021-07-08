namespace UnityMultimediaStreaming.Scripts.Audio
{
    /// <summary>
    /// Abstract module for handling networking.
    /// </summary>
    public abstract class VoiceChatNetworkModule
    {
        /// <summary>
        /// Your id, must be unique for the entire network.
        /// </summary>
        protected int Id { get; }
        
        /// <summary>
        /// The server URI
        /// </summary>
        protected string ServerUrl { get; }
        
        /// <summary>
        /// The audio format for both sent and received audio.
        /// </summary>
        protected AudioFormat AudioFormat { get; set; }
        
        /// <summary>
        /// The audio frame buffer to write to, this is where you will get the downloaded frames from.
        /// </summary>
        public AudioFrameBuffer AudioFrameBuffer { get; set; }

        /// <summary>
        /// The audio codec used in the transmission. Can be null for uncompressed data.
        /// </summary>
        protected AudioCodec AudioCodec { get; }

        /// <summary>
        /// Constructor for the networking module.
        /// </summary>
        /// <param name="id">Your unique id so people can tell who the frames come from.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <param name="audioFormat">The audio format for this voice chat instance.</param>
        /// <param name="audioCodec">The audio codec for compression.</param>
        protected VoiceChatNetworkModule(int id, string serverUri, AudioFormat audioFormat, AudioCodec audioCodec)
        {
            Id = id;
            ServerUrl = serverUri;
            AudioFormat = audioFormat;
            AudioCodec = audioCodec;
            VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Created " + this);
        }

        /// <summary>
        /// Start listening for frames. The result is written in the given buffer.
        /// This should create a new thread to run on. 
        /// </summary>
        /// <param name="audioFrameBuffer">The buffer where we write the incoming frames.</param>
        public abstract void StartListenForFrames(AudioFrameBuffer audioFrameBuffer);
        
        /// <summary>
        /// Stop the thread listening for new frames.
        /// In Unity you are gonna want to call this in OnDestroy().
        /// </summary>
        public abstract void StopListenForFrames();

        /// <summary>
        /// Send a frame to the network.
        /// </summary>
        /// <param name="frame">The frame to send.</param>
        /// <param name="newCodecState">If the codec state should be reset, this will be done at the receiving end too.</param>
        public abstract void SendFrame(short[] frame, bool newCodecState = false);

        public override string ToString()
        {
            return base.ToString() + "[id:"+ Id + ", serverUri:" + ServerUrl + ", codec:" + AudioCodec + "]";
        }
    }
}