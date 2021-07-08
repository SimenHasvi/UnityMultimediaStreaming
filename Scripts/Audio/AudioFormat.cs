namespace UnityMultimediaStreaming.Scripts.Audio
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
        /// <param name="frameSize">Milliseconds per frame. (You can also use samples per frame with <see cref="frameSizeInMs"/>)</param>
        /// <param name="channels">Channels, in general you should just leave this one alone.</param>
        /// <param name="frameSizeInMs">This allows you to specify if the framesize is given in ms or samples, you are generally gonna want to leave this alone.</param>
        public AudioFormat(int samplingRate = 16000, int frameSize = 20, int channels = 1, bool frameSizeInMs = true) : this()
        {
            //TODO: maybe its best to use float numbers here?
            SamplingRate = samplingRate;
            Channels = channels;
            if (frameSizeInMs)
            {
                MillisecondsPerFrame = frameSize;
                FramesPerSecond = 1000 / MillisecondsPerFrame;
                SamplesPerFrame = samplingRate / FramesPerSecond;
            }
            else
            {
                SamplesPerFrame = frameSize;
                MillisecondsPerFrame = 1000 / (SamplingRate / SamplesPerFrame);
                FramesPerSecond = 1000 / MillisecondsPerFrame;
            }
        }

        /// <summary>
        /// Give the number of samples in the given time span.
        /// </summary>
        /// <param name="ms">The time span in milliseconds</param>
        /// <returns>The number of samples in the time span.</returns>
        public int SamplesInMs(int ms)
        {
            return (int)(SamplingRate / (1000f / ms));
        }

        public override string ToString()
        {
            return "AudioFormat[sampling rate:" + SamplingRate + ", ms per frame:" + MillisecondsPerFrame + "]";
        }
    }
}
