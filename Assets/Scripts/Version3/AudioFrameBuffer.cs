using System.Collections.Generic;
using UnityEngine;

namespace Version3
{
    /// <summary>
    /// A simple class to buffer frames easily.
    /// </summary>
    public class AudioFrameBuffer
    {
        /// <summary>
        /// The number of frames allowed in the buffer.
        /// If you try to add more, frames will be discarded to make space.
        /// Its recommended to set manually based on the frame size with <see cref="SetBufferSizeMs"/>.
        /// </summary>
        public int MaxFramesInBuffer { get; set; } = 10;
        
        /// <summary>
        /// The audio format used in this buffer.
        /// </summary>
        private AudioFormat _audioFormat;
        
        /// <summary>
        /// The backend data structure to store the frames, you should never have to deal with this.
        /// </summary>
        private readonly Dictionary<int, Queue<short[]>> _frameBuffers = new Dictionary<int, Queue<short[]>>();
        
        /// <summary>
        /// Create the buffer object.
        /// </summary>
        /// <param name="audioFormat">The audio format stored in this buffer.</param>
        public AudioFrameBuffer(AudioFormat audioFormat)
        {
            _audioFormat = audioFormat;
            if (_audioFormat.MillisecondsPerFrame <= 100) SetBufferSizeMs(500); //allow half a second by default
        }
        
        /// <summary>
        /// Set the number of allowed frames in the buffer to match a given amount of milliseconds.
        /// </summary>
        /// <param name="targetMs">The amount of milliseconds we should allow to buffer</param>
        public void SetBufferSizeMs(int targetMs)
        {
            Debug.Assert(targetMs > _audioFormat.MillisecondsPerFrame, "The buffer has to be longer than a frame!");
            MaxFramesInBuffer = targetMs / _audioFormat.MillisecondsPerFrame;
        }

        /// <summary>
        /// Add a frame to the buffer. Get the frame later with <see cref="GetNextFrameFromBuffer"/>.
        /// <remarks>
        /// If there are multiple concurrent audio streams from different users, use the id to separate them.
        /// Then mix them together with <see cref="GetNextFrameFromBuffer"/>
        /// If the server mix the streams together, you can ignore this and leave it at the default value 0.
        /// </remarks>
        /// </summary>
        /// <param name="frame">The frame add.</param>
        /// <param name="id">The id of the user where the frame comes from, ignore if no such thing.</param>
        public void AddFrameToBuffer(short[] frame, int id = 0)
        {
            if (!_frameBuffers.ContainsKey(id)) _frameBuffers.Add(id, new Queue<short[]>());
            if (_frameBuffers[id].Count > MaxFramesInBuffer) while (_frameBuffers[id].Count > MaxFramesInBuffer / 2) _frameBuffers[id].Dequeue();
            _frameBuffers[id].Enqueue(frame);
        }

        /// <summary>
        /// Get the next frame from the buffer.
        /// </summary>
        /// <param name="excludeId">ID's to not include in the frame. Used to mute someone or exclude your own voice.</param>
        /// <returns>The next frame.</returns>
        public short[] GetNextFrameFromBuffer(params int[] excludeId)
        {
            var combinedFrame = new short[_audioFormat.SamplesPerFrame];
            foreach (var frameBuffer in _frameBuffers.Values)
            {
                lock (frameBuffer)
                {
                    if (frameBuffer.Count <= 0) continue;
                    var frame = frameBuffer.Dequeue();
                    for (var i = 0; i < combinedFrame.Length; i++)
                    {
                        combinedFrame[i] += frame[i];
                    }
                }
            }
            return combinedFrame;
        }
    }
}