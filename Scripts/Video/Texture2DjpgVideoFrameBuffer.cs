using UnityEngine;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    
    /// <summary>
    /// Buffer for frames that are encoded in JPG format.
    /// </summary>
    public class Texture2DjpgVideoFrameBuffer : VideoFrameBuffer
    {
        public override void GetNextFrameFromBuffer(int id, Texture2D texture)
        {
            texture.LoadImage(GetNextFrameFromBuffer(id));
        }
    }
}