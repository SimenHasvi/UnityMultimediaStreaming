using System.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace VoiceChat
{
    /// <summary>
    /// Class for handling audio playback in unity.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioPlayback : MonoBehaviour
    {
        private int _endOfData = 0;
        private AudioSource _audioSource;
        private AudioFormat _audioFormat;
        private AudioFrameBuffer _audioFrameBuffer;

        /// <summary>
        /// Start playing the audio.
        /// </summary>
        /// <param name="audioFormat">The audio format to play</param>
        /// <param name="audioFrameBuffer">If this is provided it will automatically add frames from this buffer.</param>
        public void Play(AudioFormat audioFormat, AudioFrameBuffer audioFrameBuffer = null)
        {
            _audioFormat = audioFormat;
            _audioFrameBuffer = audioFrameBuffer;
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = AudioClip.Create("voice_chat_clip", _audioFormat.SamplesPerFrame * 10, 1, _audioFormat.SamplingRate, false);
            _audioSource.loop = true;
            _audioSource.Play();
            StartCoroutine(PlayFramesFromBuffer());
        }

        /// <summary>
        /// Plays frames from the buffer.
        /// It will add frames to the circular AudioSource buffer about 3 frames in advance.
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayFramesFromBuffer()
        {
            while (true)
            {
                PlayFrame(_audioFrameBuffer.GetNextFrameFromBuffer());
                while (VoiceChatUtils.CircularDistanceTo(_audioSource.timeSamples, _endOfData, _audioSource.clip.samples) > _audioFormat.SamplesPerFrame * 3) yield return null;
            }
        }

        /// <inheritdoc cref="PlayFrame(float[])"/>
        public void PlayFrame(short[] frame)
        {
            PlayFrame(VoiceChatUtils.ShortToFloat(frame));
        }
        
        /// <summary>
        /// Schedule a frame to be played.
        /// <remarks>Expect ~200ms delay before its actually played on the speakers.</remarks>
        /// </summary>
        /// <param name="frame">The frame to play.</param>
        public void PlayFrame(float[] frame)
        {
            _audioSource.clip.SetData(frame, _endOfData);
            _endOfData += _audioFormat.SamplesPerFrame;
            if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
        }
        
        /// <summary>
        /// Get the last played samples. This should match closely to what is played by the soundcard.
        /// Ideal for the use in echo cancellation.
        /// </summary>
        /// <returns> The last played samples, the length is that of a frame.</returns>
        public short[] GetLastPlayedFrame()
        {
            var playbackPos = _audioSource.timeSamples - _audioFormat.SamplesPerFrame;
            if (playbackPos < 0) playbackPos = _audioSource.clip.samples + playbackPos;
            var prevFrame = new float[_audioFormat.SamplesPerFrame];
            _audioSource.clip.GetData(prevFrame, playbackPos);
            return VoiceChatUtils.FloatToShort(prevFrame);
        }
    }
}