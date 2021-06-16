namespace VoiceChat
{
    /// <summary>
    /// Contains information about a given audio format such as sample rate and frames.
    /// </summary>
    public struct AudioFormat
    {
        /// <summary>
        /// Samples per second.
        /// </summary>
        public int SamplingRate {get;}
        
        /// <summary>
        /// Milliseconds per frame.
        /// </summary>
        public int MillisecondsPerFrame {get;}
        
        /// <summary>
        /// Number of samples per frame.
        /// </summary>
        public int SamplesPerFrame {get;}
        
        /// <summary>
        /// The amount of samples per second.
        /// </summary>
        public int FramesPerSecond {get;}
        
        /// <summary>
        /// The number or channels. This should pretty much always be 1 in the context of voice chat.
        /// </summary>
        public int Channels { get; }
        
        /// <summary>
        /// Constructor for the AudioFormat object.
        /// </summary>
        /// <param name="samplingRate">Samples per second.</param>
        /// <param name="millisecondsPerFrame">Milliseconds per frame.</param>
        public AudioFormat(int samplingRate, int millisecondsPerFrame, int channels = 1) : this()
        {
            
            SamplingRate = samplingRate;
            MillisecondsPerFrame = millisecondsPerFrame;
            FramesPerSecond = 1000 / MillisecondsPerFrame;
            SamplesPerFrame = samplingRate / FramesPerSecond;
            Channels = channels;
        }
    }
}
