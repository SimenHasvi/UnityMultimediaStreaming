using System;
using System.Runtime.InteropServices;

namespace VoiceChat
{
    /// <inheritdoc />
    public class SpeexDspAudioProcessor : AudioProcessor
    {
        private bool _performAec = false;
        private IntPtr _preprocessState;
        private IntPtr _aecState;
        
        public SpeexDspAudioProcessor(AudioFormat audioFormat, bool denoise, bool agc, bool vad, bool aec) : base(audioFormat, denoise, agc, vad, aec)
        {
            _preprocessState = SpeexDSPNative.speex_preprocess_state_init(AudioFormat.SamplesPerFrame, AudioFormat.SamplingRate);
            PreprocessCtlRequest.Request(_preprocessState, denoise, agc, vad);
            PreprocessCtlRequest.SetAgcLevel(_preprocessState, 100000f);
            if (_performAec = aec)
            {
                _aecState = SpeexDSPNative.speex_echo_state_init(AudioFormat.SamplesPerFrame, 100);
                PreprocessCtlRequest.SetAecState(_preprocessState, _aecState);
            }
        }
        
        public override bool ProcessFrame(short[] frame)
        {
            var vadResult = SpeexDSPNative.speex_preprocess_run(_preprocessState, frame);
            if (_performAec) SpeexDSPNative.speex_echo_capture(_aecState, frame, frame);
            return vadResult == 0;
        }

        public override void RegisterPlayedFrame(short[] frame)
        {
            if (_performAec) SpeexDSPNative.speex_echo_playback(_aecState, frame);
        }
    }

    /// <summary>
    /// Contains helper functions to make requests to the preprocessor state.
    /// If you want to add more of these yourself, the request integers can be found in the source header files.
    /// </summary>
    public static unsafe class PreprocessCtlRequest
    {
        /// <summary>
        /// Make multiple request to the native preprocess state.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="denoise"> Enable denoise. </param>
        /// <param name="agc">Enable automatic gain control. Control the gain level with: <see cref="SetAgcLevel"/></param>
        /// <param name="vad">Enable voice activity detection.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool Request(IntPtr preprocessState, bool denoise, bool agc, bool vad)
        {
            return SetDenoise(preprocessState, denoise) && SetAgc(preprocessState, agc) && SetVad(preprocessState, vad);
        }
    
        /// <summary>
        /// Make a request to the given state to enable/disable denoising.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="value">Whether to enable or disable.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetDenoise(IntPtr preprocessState, bool value = true)
        {
            var instruction = value ? 1 : 0;
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 0, new IntPtr(&instruction));
            return result == 0;
        }
    
        /// <summary>
        /// Make a request to the given state to enable/disable automatic gain control.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="value">Whether to enable or disable.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetAgc(IntPtr preprocessState, bool value = true)
        {
            var instruction = value ? 1 : 0;
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 2, new IntPtr(&instruction));
            return result == 0;
        }
    
        /// <summary>
        /// Make a request to the given state to enable/disable voice activity detection.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="value">Whether to enable or disable.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetVad(IntPtr preprocessState, bool value = true)
        {
            var instruction = value ? 1 : 0;
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 4, new IntPtr(&instruction));
            return result == 0;
        }
    
        /// <summary>
        /// Make a request to the given state to set the automatic gain control level.
        /// Setting this will define the volume level in which the gain is altered to achieve.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="level">The level to aim for. Im not sure on the scale as they make no reference in the documentation, but I found it to be good in the thousands. The default is 8000.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetAgcLevel(IntPtr preprocessState, float level)
        {
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 6, new IntPtr(&level));
            return result == 0;
        }
    
        /// <summary>
        /// Get the agc level, use <see cref="SetAgcLevel"/> to set it.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <returns>The agc level.</returns>
        public static float GetAgcLevel(IntPtr preprocessState)
        {
            var level = 0f;
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 7, new IntPtr(&level));
            return level;
        }

        /// <summary>
        /// Provide a reference of the echo canceller state to the preprocessor state.
        /// If you perform echo cancellation you are gonna want to do this for better results.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="aecState">The echo canceller state.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetAecState(IntPtr preprocessState, IntPtr aecState)
        {
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 24, aecState);
            return result == 0;
        }
    }

    /// <summary>
    /// Native functions from the speexdsp library.
    /// See the original documentation if you are curious.
    /// <remarks>The IntPtr objects are pointers to native objects.</remarks>
    /// </summary>
    public static class SpeexDSPNative
    {
        [DllImport("speexdsp")]
        public static extern IntPtr speex_preprocess_state_init(int frame_size, int sampling_rate);
        
        [DllImport("speexdsp")]
        public static extern int speex_preprocess_ctl(IntPtr preprocess_state, int request, IntPtr ptr);
    
        [DllImport("speexdsp")]
        public static extern int speex_preprocess_run(IntPtr preprocess_state, short[] audio_frame);
        
        [DllImport("speexdsp")]
        public static extern IntPtr speex_echo_state_init(int frame_size, int filter_length);
        
        [DllImport("speexdsp")]
        public static extern void speex_echo_cancellation(IntPtr echo_state, short[] input_frame, short[] echo_frame, short[] output_frame);
        
        [DllImport("speexdsp")]
        public static extern void speex_echo_playback(IntPtr echo_state, short[] echo_frame);
        
        [DllImport("speexdsp")]
        public static extern void speex_echo_capture(IntPtr echo_state, short[] input_frame, short[] output_frame);
    }
}