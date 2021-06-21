using System;
using System.Runtime.InteropServices;

namespace VoiceChat
{
    /// <inheritdoc />
    public class SpeexDspAudioProcessor : AudioProcessor
    {
        private readonly bool _performAec = false;
        private readonly IntPtr _preprocessState;
        private readonly IntPtr _aecState;
        
        public SpeexDspAudioProcessor(AudioFormat audioFormat, bool denoise, bool agc, bool vad, bool aec, bool deReverb, int aecFilterLengthMs = 100) : base(audioFormat, denoise, agc, vad, aec)
        {
            _preprocessState = SpeexDSPNative.speex_preprocess_state_init(AudioFormat.SamplesPerFrame, AudioFormat.SamplingRate);
            PreprocessCtlRequest.Configure
            (
                _preprocessState, 
                denoise: denoise, 
                agc: agc, 
                vad: vad, 
                deReverb: deReverb, 
                agcTarget: 150000
            );
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, PreprocessCtlRequest.PrintConfigurations(_preprocessState));
            if (_performAec = aec)
            {
                // speexdsp recommends 100ms filter length for a small room, but make sure it is not too long
                _aecState = SpeexDSPNative.speex_echo_state_init(AudioFormat.SamplesPerFrame, AudioFormat.SamplesInMs(aecFilterLengthMs));
                PreprocessCtlRequest.SetAecState(_preprocessState, _aecState);
                VoiceChatUtils.Log(VoiceChatUtils.LogType.VerboseInfo, "Enabled aec with frame size" + AudioFormat.SamplesPerFrame + " and filter length ms " + aecFilterLengthMs);
                if (Math.Sqrt(AudioFormat.SamplesPerFrame) % 1 != 0) VoiceChatUtils.Log(VoiceChatUtils.LogType.Warning, "Optimal aec frame size should be a power of 2 in the order of 20ms.");
            }
        }

        public override bool ProcessFrame(short[] frame)
        {
            var vadResult = SpeexDSPNative.speex_preprocess_run(_preprocessState, frame);
            if (_performAec) SpeexDSPNative.speex_echo_capture(_aecState, frame, frame);
            return vadResult == 0;
        }

        // if you use this dont register the echo frame! We already have it here.
        public override bool ProcessFrame(short[] frame, short[] echoFrame)
        {
            var vadResult = SpeexDSPNative.speex_preprocess_run(_preprocessState, frame);
            if (_performAec) SpeexDSPNative.speex_echo_cancellation(_aecState, frame, echoFrame, frame);
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
        /// <param name="deReverb">Enable de reverb.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool Enable(IntPtr preprocessState, bool denoise, bool agc, bool vad, bool deReverb)
        {
            return SetDenoise(preprocessState, denoise) && SetAgc(preprocessState, agc) && SetVad(preprocessState, vad) && SetDeReverb(preprocessState, deReverb);
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
        /// Turns reverberation removal on or off.
        /// </summary>
        /// <param name="preprocessState">The state where we make the requests.</param>
        /// <param name="value">To turn it on or off.</param>
        /// <returns>Whether the requests where successful or not.</returns>
        public static bool SetDeReverb(IntPtr preprocessState, bool value = true)
        {
            var instruction = value ? 1 : 0;
            var result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 8, new IntPtr(&instruction));
            return result == 0;
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

        /// <summary>
        /// Configure the preprocess state, and all its features.
        /// If you only want to enable some stuff then use <see cref="Enable"/> instead.
        /// This function is if you want to customize more advanced settings.
        /// The default values are set to the default of speexdps, so just leave the setting alone if you dont intend to change it.
        /// </summary>
        /// <param name="preprocessState">The state to apply all the settings to.</param>
        /// <param name="denoise">Set preprocessor denoiser state</param>
        /// <param name="agc">Set preprocessor Automatic Gain Control state</param>
        /// <param name="vad">Set preprocessor Voice Activity Detection state</param>
        /// <param name="agcLevel">Set preprocessor Automatic Gain Control level (float)</param>
        /// <param name="deReverb">Set preprocessor dereverb state</param>
        /// <param name="deReverbLevel">Set preprocessor dereverb level</param>
        /// <param name="deReverbDecay">Set preprocessor dereverb decay</param>
        /// <param name="vadProbStart">Set probability required for the VAD to go from silence to voice</param>
        /// <param name="vadProbContinue">Set probability required for the VAD to stay in the voice state (integer percent)</param>
        /// <param name="noiseSuppress">Set maximum attenuation of the noise in dB (negative number)</param>
        /// <param name="echoSuppress">Set maximum attenuation of the residual echo in dB (negative number)</param>
        /// <param name="echoSuppressActive">Set maximum attenuation of the residual echo in dB when near end is active (negative number)</param>
        /// <param name="agcIncrement">Set maximal gain increase in dB/second</param>
        /// <param name="agcDecrement">Set maximal gain decrease in dB/second</param>
        /// <param name="agcMaxGain">Set maximal gain in dB (int32)</param>
        /// <param name="agcTarget">Set preprocessor Automatic Gain Control level. (same as agc level??)</param>
        /// <returns>Whether all the settings where applied correctly.</returns>
        public static bool Configure
        (
            IntPtr preprocessState,
            bool denoise = false,
            bool agc = false,
            bool vad = false,
            float agcLevel = 0,
            bool deReverb = false,
            int deReverbLevel = 0,
            int deReverbDecay = 0,
            int vadProbStart = 35,
            int vadProbContinue = 20,
            int noiseSuppress = -15,
            int echoSuppress = -40,
            int echoSuppressActive = -15,
            int agcIncrement = 12,
            int agcDecrement = -40,
            int agcMaxGain = 30,
            int agcTarget = 8000
        )
        {
            var returnValue = true;
            var result = 0;
            var intInstruction = 0;

            intInstruction = denoise ? 1 : 0;
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 0, new IntPtr(&intInstruction));
            if (result != 0) returnValue = false;
            
            intInstruction = agc ? 1 : 0;
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 2, new IntPtr(&intInstruction));
            if (result != 0) returnValue = false;
            
            intInstruction = vad ? 1 : 0;
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 4, new IntPtr(&intInstruction));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 6, new IntPtr(&agcLevel));
            if (result != 0) returnValue = false;
            
            intInstruction = deReverb ? 1 : 0;
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 8, new IntPtr(&intInstruction));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 10, new IntPtr(&deReverbLevel));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 12, new IntPtr(&deReverbDecay));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 14, new IntPtr(&vadProbStart));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 16, new IntPtr(&vadProbContinue));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 18, new IntPtr(&noiseSuppress));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 20, new IntPtr(&echoSuppress));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 22, new IntPtr(&echoSuppressActive));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 26, new IntPtr(&agcIncrement));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 28, new IntPtr(&agcDecrement));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 30, new IntPtr(&agcMaxGain));
            if (result != 0) returnValue = false;
            
            result = SpeexDSPNative.speex_preprocess_ctl(preprocessState, 46, new IntPtr(&agcTarget));
            if (result != 0) returnValue = false;
            
            return returnValue;
        }

        /// <summary>
        /// Get a string with all the settings of this preprocessor.
        /// This can be printed out for debugging.
        /// See the speexdsp documentation and headerfiles to learn what it all means.
        /// </summary>
        /// <param name="preprocessState">The state to get the configurations from.</param>
        /// <returns>A string with all the configurations.</returns>
        public static string PrintConfigurations(IntPtr preprocessState)
        {
            var str = "";
            var floatVar = 0f;
            var intVar = 0;
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 1, new IntPtr(&intVar));
            str += "denoise: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 3, new IntPtr(&intVar));
            str += "agc: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 5, new IntPtr(&intVar));
            str += "vad: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 7, new IntPtr(&floatVar));
            str += "agc level: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 9, new IntPtr(&intVar));
            str += "dereverb: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 11, new IntPtr(&intVar));
            str += "dereverb level: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 13, new IntPtr(&intVar));
            str += "dereverb decay: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 15, new IntPtr(&intVar));
            str += "vad prob start: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 17, new IntPtr(&intVar));
            str += "vad prob continue: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 19, new IntPtr(&intVar));
            str += "noise suppress: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 21, new IntPtr(&intVar));
            str += "echo suppress: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 23, new IntPtr(&intVar));
            str += "echo suppress active: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 27, new IntPtr(&intVar));
            str += "agc increment: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 29, new IntPtr(&intVar));
            str += "agc decrement: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 31, new IntPtr(&intVar));
            str += "agc max gain: " + intVar + "\n";
            SpeexDSPNative.speex_preprocess_ctl(preprocessState, 47, new IntPtr(&intVar));
            str += "agc target: " + intVar + "\n";
            return str;
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