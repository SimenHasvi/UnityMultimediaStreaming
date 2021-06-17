using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat
{
    public class VoiceChat : MonoBehaviour
    {
        public bool playSelf = false;
        public int id = 0;
        public string serverUri;
        public string serverTopic;
        public bool localNetwork = true;
        
        public int sampleRate = 16000;
        public int millisecondsPerFrame = 20;
        public int millisecondsInBuffer = 500;
        public bool denoise = true;
        public bool automaticGainControl = true;
        public bool voiceActivityDetector = true;
        public bool acousticEchoCancellation = true;
        public int aecFilterLengthMs = 100;

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
            
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, denoise, automaticGainControl, voiceActivityDetector, acousticEchoCancellation, aecFilterLengthMs);

            if (localNetwork) _networkModule = new LocalVoiceChatNetworkModule(_audioCodec);
            else _networkModule = new KafkaVoiceChatNetworkModule(id, serverUri, serverTopic, _audioCodec);
            _networkModule.StartListenForFrames(_audioFormat, _audioFrameBuffer);
            
            gameObject.AddComponent<AudioPlayback>();
            _audioPlayback = GetComponent<AudioPlayback>();
            _audioPlayback.Play(_audioFormat, _audioFrameBuffer);
            if (!playSelf) _audioPlayback.Mute(id);
            _mic = Microphone.Start(Microphone.devices[0], true, 50, sampleRate);

            StartCoroutine(SampleAudio());
        }
        
        private IEnumerator SampleAudio()
        {
            while (true)
            {
                var frame = new float[_audioFormat.SamplesPerFrame];
                while (_pos - _lastPos < frame.Length)
                {
                    _pos = Microphone.GetPosition(Microphone.devices[0]);
                    if (_pos < _lastPos) _lastPos = 0;
                    yield return null;
                }

                _mic.GetData(frame, _lastPos);
                _lastPos += frame.Length;
                var shortFrame = VoiceChatUtils.FloatToShort(frame);
                _audioProcessor.ProcessFrame(shortFrame, _audioPlayback.GetLastPlayedFrame());
                _networkModule.SendFrame(shortFrame);
            }
        }

        private void OnDisable()
        {
            _networkModule.StopListenForFrames();
        }
    }
}