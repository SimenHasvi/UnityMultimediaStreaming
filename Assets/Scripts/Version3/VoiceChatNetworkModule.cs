using UnityEditor;

namespace Version3
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
        protected string ServerUri { get; }
        
        /// <summary>
        /// The audio format for both sent and received audio.
        /// </summary>
        protected AudioFormat AudioFormat { get; }
        
        /// <summary>
        /// The audio frame buffer to write to, this is where you will get the downloaded frames from.
        /// </summary>
        protected AudioFrameBuffer AudioFrameBuffer { get; }
        
        /// <summary>
        /// The audio codec used in the transmission. Can be null for uncompressed data.
        /// </summary>
        protected AudioCodec AudioCodec { get; }
        
        /// <summary>
        /// Constructor for the networking module.
        /// </summary>
        /// <param name="id">Your unique id so people can tell who the frames come from.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <param name="audioCodec">The audio codec for compression.</param>
        protected VoiceChatNetworkModule(int id, string serverUri, AudioCodec audioCodec = null)
        {
            Id = id;
            ServerUri = serverUri;
        }
        
        /// <summary>
        /// Start listening for frames. The result is written in the given buffer.
        /// This should create a new thread to run on. 
        /// </summary>
        /// <param name="audioFormat">The format of the incoming audio frames.</param>
        /// <param name="audioFrameBuffer">The buffer where we write the incoming frames.</param>
        public abstract void StartListenForFrames(AudioFormat audioFormat, AudioFrameBuffer audioFrameBuffer);
        
        /// <summary>
        /// Stop the thread listening for new frames.
        /// In Unity you are gonna want to call this in OnDestroy().
        /// </summary>
        public abstract void StopListenForFrames();
        
        /// <summary>
        /// Send a frame to the network.
        /// </summary>
        /// <param name="frame">The frame to send.</param>
        public abstract void SendFrame(short[] frame);
    }
}