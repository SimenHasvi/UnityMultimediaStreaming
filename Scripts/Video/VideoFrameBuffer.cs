using System.Collections.Generic;
using UnityEngine;
using UnityMultimediaStreaming.Scripts.Audio;

namespace UnityMultimediaStreaming.Scripts.Video
{
    /// <summary>
    /// Class to help manage video streams.
    /// We assume that we only need to buffer a single frame per stream as smooth playback is not the priority.
    /// </summary>
    public abstract class VideoFrameBuffer
    {
        protected Dictionary<int, byte[]> _buffers = new Dictionary<int, byte[]>();

        /// <summary>
        /// Get the id's of the buffers.
        /// You might want to check this regularly to see of any new ones are added.
        /// </summary>
        /// <returns>The id's of the buffers.</returns>
        public IEnumerable<int> GetActiveBufferIds()
        {
            return _buffers.Keys;
        }

        /// <summary>
        /// Add a frame to the buffer. Get the frame later with <see cref="GetNextFrameFromBuffer"/>.
        /// <remarks>
        /// If there are multiple concurrent video streams from different users, use the id to separate them.
        /// </remarks>
        /// TODO: This will create a new array. Is there a way to reuse the same one over and over?
        /// </summary>
        /// <param name="frame">The frame add.</param>
        /// <param name="id">The id of the user where the frame comes from.</param>
        public void AddFrameToBuffer(byte[] frame, int id)
        {
            if (!_buffers.ContainsKey(id))
            {
                _buffers.Add(id, frame);
                VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Added new buffer for user: " + id);
            }
            else
            {
                _buffers[id] = frame;
            }
        }

        /// <summary>
        /// Get the buffer for the given id.
        /// </summary>
        /// <param name="id">The id to retrieve the buffer from.</param>
        /// <returns>The buffer.</returns>
        protected byte[] GetNextFrameFromBuffer(int id)
        {
            if (!_buffers.ContainsKey(id))
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.Warning, "Tried to access non-existent buffer!");
                return new byte[0];
            }
            return _buffers[id];
        }

        /// <summary>
        /// Get the buffer for the given id.
        /// </summary>
        /// <param name="id">The id to retrieve the buffer from.</param>
        /// <returns>The buffer.</returns>
        public abstract void GetNextFrameFromBuffer(int id, Texture2D texture);

    }
}