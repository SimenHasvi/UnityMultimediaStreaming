using System.Collections;
using UnityEngine;

namespace UnityMultimediaStreaming.Scripts.Audio
{
    /// <summary>
    /// Abstract class for an audio processor.
    /// </summary>
    public abstract class AudioProcessor
    {
        /// <summary>
        /// The audio format we process.
        /// </summary>
        protected AudioFormat AudioFormat { get; }

        /// <summary>
        /// Describes which features of this audio processor is enabled/disabled.
        /// </summary>
        public bool Denoise, Agc, Vad, Aec;

        /// <summary>
        /// Simple constructor.
        /// </summary>
        /// <param name="audioFormat">The format we process</param>
        /// <param name="aec">Enable acoustic echo cancellation.</param>
        protected AudioProcessor(AudioFormat audioFormat, bool aec)
        {
            AudioFormat = audioFormat;
            Aec = aec;
            VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Created " + this);
        }

        /// <summary>
        /// Process a frame with the configured settings.
        /// Do this BEFORE you send the the recorded frame.
        /// Call <see cref="RegisterPlayedFrame"/> just before you playback the frame (if needed).
        /// </summary>
        /// <param name="frame">The frame to process.</param>
        /// <param name="outFrame">The frame where we write the results.</param>
        /// <returns>VAD result, is always true if no VAD.</returns>
        public abstract bool ProcessFrame(short[] frame, short[] outFrame);

        /// <summary>
        /// Process a frame with the configured settings.
        /// Do this BEFORE you send the the recorded frame.
        /// <remarks>
        /// If you use this rather than <see cref="ProcessFrame(short[])"/>, you should NOT register the frame when playing it.
        /// As we already have the frame played here.
        /// </remarks>
        /// </summary>
        /// <param name="frame">The frame to process.</param>
        /// <param name="echoFrame">The audio that was played at the same time as the input frame was recorded.</param>
        /// <param name="outFrame">The frame where we write the results.</param>
        /// <returns>VAD result, is always true if no VAD.</returns>
        public abstract bool ProcessFrame(short[] frame, short[] echoFrame, short[] outFrame);

        /// <summary>
        /// Register a frame that you play.
        /// This is used by the echo canceller.
        /// </summary>
        /// <param name="frame">The frame to register.</param>
        public abstract void RegisterPlayedFrame(short[] frame);
        
        /// <summary>
        /// Wait for a given amount of time before calling <see cref="RegisterPlayedFrame"/>
        /// This is supposed to be called as a coroutine in from a Unity monobehaviour object.
        /// This is useful as we can register the frame after it has been scheduled to play. Accounting for the delay.
        /// </summary>
        /// <param name="frame">The frame to register.</param>
        /// <param name="delayMs">The delay before we register the frame.</param>
        /// <returns>An enumerator for the StartCoroutine method of monobehaviour./></returns>
        public IEnumerator RegisterPlayedFrameDelayed(short[] frame, int delayMs)
        {
            yield return new WaitForSecondsRealtime(delayMs / 1000f);
            RegisterPlayedFrame(frame);
        }

        public override string ToString()
        {
            return base.ToString() + "[denoise:" + Denoise + ", agc:" + Agc + ", vad:" + Vad + ", aec:" + Aec + ", format:" + AudioFormat + "]";
        }
    }
}