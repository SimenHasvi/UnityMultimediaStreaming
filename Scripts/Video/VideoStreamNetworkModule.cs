using UnityEngine;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    public abstract class VideoStreamNetworkModule
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
        /// The audio frame buffer to write to, this is where you will get the downloaded frames from.
        /// </summary>
        public VideoFrameBuffer VideoFrameBuffer { get; set; }
        
        /// <summary>
        /// Constructor for the networking module.
        /// </summary>
        /// <param name="id">Your unique id so people can tell who the frames come from.</param>
        /// <param name="serverUri">The server URI.</param>
        /// <param name="audioCodec">The audio codec for compression.</param>
        protected VideoStreamNetworkModule(int id, string serverUri)
        {
            Id = id;
            ServerUri = serverUri;
            VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Created " + this);
        }

        /// <summary>
        /// Start listening for frames. The result is written in the given buffer.
        /// This should create a new thread to run on. 
        /// </summary>
        /// <param name="videoFrameBuffer">The buffer where we write the incoming frames.</param>
        public abstract void StartListenForFrames(VideoFrameBuffer videoFrameBuffer);
        
        /// <summary>
        /// Stop the thread listening for new frames.
        /// In Unity you are gonna want to call this in OnDestroy().
        /// </summary>
        public abstract void StopListenForFrames();

        /// <summary>
        /// Send a frame to the network.
        /// </summary>
        /// <param name="frame">The frame to send.</param>
        public abstract void SendFrame(byte[] frame);
        
        /// <summary>
        /// Encodes the jpg from the texture and calls <see cref="SendFrame"/>
        /// </summary>
        /// <param name="texture">the texture containing the frame to send.</param>
        public void SendFrameJpg(Texture2D texture)
        {
            SendFrame(texture.EncodeToJPG());
        }

        public override string ToString()
        {
            return base.ToString() + "[id:"+ Id + ", serverUri:" + ServerUri + "]";
        }
    }
}