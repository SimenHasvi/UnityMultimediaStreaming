using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityVoiceChat.Scripts.Audio;

namespace VoiceChat
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
        /// Get the users with a buffer.
        /// </summary>
        /// <returns>List of the users.</returns>
        public IEnumerable<int> GetUsers()
        {
            return _frameBuffers.Keys;
        }

        /// <summary>
        /// Get the count of the buffer.
        /// If there are multiple users this is the longest of them all.
        /// </summary>
        /// <returns>Length of the buffer.</returns>
        public int Count()
        {
            return _frameBuffers.Values.Select(b => b.Count).Prepend(0).Max();
        }

        /// <summary>
        /// Length of the buffer.
        /// </summary>
        /// <param name="id">The id of the buffer to get the length from.</param>
        /// <returns>The length.</returns>
        public int Count(int id)
        {
            return _frameBuffers[id].Count;
        }
        
        /// <summary>
        /// Set the number of allowed frames in the buffer to match a given amount of milliseconds.
        /// </summary>
        /// <param name="targetMs">The amount of milliseconds we should allow to buffer</param>
        public void SetBufferSizeMs(int targetMs)
        {
            if (targetMs < _audioFormat.MillisecondsPerFrame)
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.Warning, "The given target is less than a single frame!");
                return;
            }
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
            if (!_frameBuffers.ContainsKey(id))
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Created a new buffer for user: " + id);
                _frameBuffers.Add(id, new Queue<short[]>());
            }
            else if (Count(id) > MaxFramesInBuffer)
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Buffer for user " + id + " is full! Skipping frames.");
                while (Count(id) > MaxFramesInBuffer / 2) _frameBuffers[id].Dequeue();
            }
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
            foreach (var frameBuffer in _frameBuffers.Where(frameBuffer => frameBuffer.Value.Count > 0))
            {
                lock (frameBuffer.Value.Peek())
                {
                    var frame = frameBuffer.Value.Dequeue();
                    if (excludeId.Contains(frameBuffer.Key)) continue;
                    for (var i = 0; i < combinedFrame.Length; i++)
                    {
                        combinedFrame[i] += frame[i];
                    }
                }
            }
            return combinedFrame;
        }

        /// <summary>
        /// Saves the entire buffer in wav files for each user.
        /// The only use for this is debugging.
        /// Especially useful for aec debugging where you can save and listen to the input and echo audio.
        /// </summary>
        /// <param name="folder">The folder to save the audio files to.</param>
        public void SaveBuffers(string folder)
        {
            foreach (var buffer in _frameBuffers)
            {
                var samples = new List<short>();
                while (buffer.Value.Count > 0) samples.AddRange(buffer.Value.Dequeue());
                SaveWav.Save(Path.Combine(folder, "" + buffer.Key), _audioFormat, VoiceChatUtils.ShortToFloat(samples.ToArray()));
            }
        }
    }
}