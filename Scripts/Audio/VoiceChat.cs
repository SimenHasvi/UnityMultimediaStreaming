using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat
{
    public class VoiceChat : MonoBehaviour
    {
        [Header("Network")]
        public bool playSelf = false;
        public int id = 0;
        public string serverUri;
        public string serverTopic;
        public bool localNetwork = true;
        
        [Header("Audio Format")]
        public int sampleRate = 16000;
        public int millisecondsPerFrame = 20;
        public int millisecondsInBuffer = 500;

        [Header("Audio Processing")]
        public bool deNoise = true;
        public bool automaticGainControl = true;
        public bool voiceActivityDetector = false;
        public bool acousticEchoCancellation = true;
        public bool deReverb = false;
        public int aecFilterLengthMs = 200;
        public float agcLevel = 0;
        public float deReverbLevel = 0;
        public float deReverbDecay = 0;
        public int vadProbStart = 95;
        public int vadProbContinue = 95;
        public int noiseSuppress = -45;
        public int echoSuppress = -40;
        public int echoSuppressActive = -15;
        public int agcIncrement = 12;
        public int agcDecrement = -40;
        public int agcMaxGain = 30;
        public int agcTarget = 10000;

        [Header("Compression")]
        public bool doCompression = true;
        public int bitrate = 14000;
        public int complexity = 10;

        private int _lastPos, _pos = 0;
        private AudioClip _mic;

        private AudioFrameBuffer _audioFrameBuffer;
        private AudioFormat _audioFormat;
        private AudioProcessor _audioProcessor;
        private AudioPlayback _audioPlayback;
        private AudioCodec _audioCodec;
        private VoiceChatNetworkModule _networkModule;


        private void Start()
        {
            VoiceChatUtils.EnableUnityLogging(true);
            
            _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame);
            
            _audioFrameBuffer = new AudioFrameBuffer(_audioFormat);
            _audioFrameBuffer.SetBufferSizeMs(millisecondsInBuffer);
            
            if (doCompression) _audioCodec = new OpusAudioCodec(_audioFormat, bitrate, complexity);
            else _audioCodec = new DummyAudioCodec(_audioFormat);
            
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, acousticEchoCancellation, aecFilterLengthMs);
            ((SpeexDspAudioProcessor)_audioProcessor).Configure
            (
                deNoise,
                automaticGainControl,
                voiceActivityDetector,
                agcLevel,
                deReverb,
                deReverbLevel,
                deReverbDecay,
                vadProbStart,
                vadProbContinue,
                noiseSuppress,
                echoSuppress,
                echoSuppressActive,
                agcIncrement,
                agcDecrement,
                agcMaxGain,
                agcTarget
            );

            if (localNetwork) _networkModule = new LocalVoiceChatNetworkModule(_audioFormat, _audioCodec);
            else _networkModule = new KafkaVoiceChatNetworkModule(id, serverUri, serverTopic, _audioFormat, _audioCodec);
            _networkModule.StartListenForFrames(_audioFrameBuffer);
            
            gameObject.AddComponent<AudioPlayback>();
            _audioPlayback = GetComponent<AudioPlayback>();
            _audioPlayback.Play(_audioFormat, _audioFrameBuffer, _audioProcessor);
            if (!playSelf) _audioPlayback.Mute(id);
            _mic = Microphone.Start(Microphone.devices[0], true, 50, sampleRate);

            StartCoroutine(SampleAudio());
        }
        
        private IEnumerator SampleAudio()
        {
            var frame = new float[_audioFormat.SamplesPerFrame];
            var shortFrame = new short[_audioFormat.SamplesPerFrame];
            var outFrame = new short[_audioFormat.SamplesPerFrame];
            while (true)
            {
                while (_pos - _lastPos < frame.Length)
                {
                    _pos = Microphone.GetPosition(Microphone.devices[0]);
                    if (_pos < _lastPos) _lastPos = 0;
                    yield return null;
                }

                _mic.GetData(frame, _lastPos);
                _lastPos += frame.Length;
                VoiceChatUtils.FloatToShort(shortFrame, frame);
                var vadResult = _audioProcessor.ProcessFrame(shortFrame, outFrame);
                if (vadResult) _networkModule.SendFrame(outFrame);
            }
        }

        private void OnDisable()
        {
            _networkModule.StopListenForFrames();
        }
    }
}