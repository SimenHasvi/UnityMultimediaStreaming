namespace Version3
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
        /// Simple constructor.
        /// </summary>
        /// <param name="audioFormat">The format we process</param>
        protected AudioProcessor(AudioFormat audioFormat, bool denoise, bool agc, bool vad, bool aec)
        {
            AudioFormat = audioFormat;
        }

        /// <summary>
        /// Process a frame with the configured settings.
        /// Do this BEFORE you send the the recorded frame.
        /// Call <see cref="RegisterPlayedFrame"/> just before you playback the frame (if needed).
        /// </summary>
        /// <param name="frame">The frame to process.</param>
        /// <returns>VAD result, is always true if no VAD.</returns>
        public abstract bool ProcessFrame(short[] frame);

        /// <summary>
        /// Register a frame that you play.
        /// This is used by the echo canceller.
        /// </summary>
        /// <param name="frame">The frame to register.</param>
        public abstract void RegisterPlayedFrame(short[] frame);
    }
}