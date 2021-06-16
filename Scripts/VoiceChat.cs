using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat
{
    public class VoiceChat : MonoBehaviour
    {
        public int id;
        public string serverUri;
        public string serverTopic;
        public bool localNetwork;
        
        public int sampleRate;
        public int millisecondsPerFrame;
        public int millisecondsInBuffer;
        public bool denoise;
        public bool automaticGainControl;
        public bool voiceActivityDetector;
        public bool acousticEchoCancellation;

        public bool doCompression;
        public int bitrate;
        public int complexity;

        private int _lastPos, _pos = 0;
        private AudioClip _mic;

        private AudioFrameBuffer _audioFrameBuffer;
        private AudioFormat _audioFormat;
        private AudioProcessor _audioProcessor;
        private AudioSource _audioSource;
        private AudioCodec _audioCodec;
        private VoiceChatNetworkModule _networkModule;
        private const int FramesInAudioSource = 50;
        private int _endOfData = 0;
        
        
        private void Start()
        {
            _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame);
            
            _audioFrameBuffer = new AudioFrameBuffer(_audioFormat);
            _audioFrameBuffer.SetBufferSizeMs(millisecondsInBuffer);
            
            if (doCompression) _audioCodec = new OpusAudioCodec(_audioFormat, bitrate, complexity);
            else _audioCodec = new DummyAudioCodec(_audioFormat);
            
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, denoise, automaticGainControl, voiceActivityDetector, acousticEchoCancellation);

            if (localNetwork) _networkModule = new LocalVoiceChatNetworkModule(_audioCodec);
            else _networkModule = new KafkaVoiceChatNetworkModule(id, serverUri, serverTopic, _audioCodec);
            _networkModule.StartListenForFrames(_audioFormat, _audioFrameBuffer);
            
            _audioSource = GetComponent<AudioSource>();
            _mic = Microphone.Start(Microphone.devices[0], true, 50, sampleRate);

            StartCoroutine(SampleAudio());
            StartCoroutine(PlayAudio());
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
                _audioProcessor.ProcessFrame(shortFrame);
                _networkModule.SendFrame(shortFrame);
            }
        }
        
        private IEnumerator PlayAudio()
        {
            _audioSource.clip = AudioClip.Create("voice_chat_clip", _audioFormat.SamplesPerFrame * FramesInAudioSource, 1, _audioFormat.SamplingRate, false);
            _audioSource.loop = true;
            _audioSource.Play();
            while (true)
            {
                var frame = _networkModule.AudioFrameBuffer.GetNextFrameFromBuffer();
                _audioProcessor.RegisterPlayedFrame(frame);
                _audioSource.clip.SetData(VoiceChatUtils.ShortToFloat(frame), _endOfData);
                _endOfData += _audioFormat.SamplesPerFrame;
                if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
                while (VoiceChatUtils.CircularDistanceTo(_audioSource.timeSamples, _endOfData, _audioSource.clip.samples) > _audioFormat.SamplesPerFrame * 5) yield return null;
            }
        }
    }
}