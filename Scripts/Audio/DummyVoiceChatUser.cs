using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VoiceChat
{
    /// <summary>
    /// A class which will send some data to the server, for debugging purposes
    /// </summary>
    public class DummyVoiceChatUser : MonoBehaviour
    {
        public AudioClip clipToPlay;
        
        public int id = 0;
        public string serverUri;
        public string serverTopic;
        
        public int sampleRate = 16000;
        public int millisecondsPerFrame = 20;
        public int millisecondsInBuffer = 500;
        public bool denoise = true;
        public bool automaticGainControl = true;
        public bool voiceActivityDetector = true;

        public bool doCompression = true;
        public int bitrate = 14000;
        public int complexity = 10;

        private int _lastPos = 0;

        private AudioFrameBuffer _audioFrameBuffer;
        private AudioFormat _audioFormat;
        private AudioProcessor _audioProcessor;
        private AudioCodec _audioCodec;
        private VoiceChatNetworkModule _networkModule;
        
        private void Start()
        {
            _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame);
            _audioFrameBuffer = new AudioFrameBuffer(_audioFormat);
            _audioFrameBuffer.SetBufferSizeMs(millisecondsInBuffer);
            
            if (doCompression) _audioCodec = new OpusAudioCodec(_audioFormat, bitrate, complexity);
            else _audioCodec = new DummyAudioCodec(_audioFormat);
            
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, denoise, automaticGainControl, voiceActivityDetector, false);
            
            _networkModule = new KafkaVoiceChatNetworkModule(id, serverUri, serverTopic, _audioCodec);

            StartCoroutine(SampleAudio());
        }
        
        private IEnumerator SampleAudio()
        {
            Debug.Assert(clipToPlay.frequency == _audioFormat.SamplingRate);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                sw.Restart();
                var frame = new float[_audioFormat.SamplesPerFrame];
                clipToPlay.GetData(frame, _lastPos);
                _lastPos += frame.Length;
                if (_lastPos >= clipToPlay.samples) _lastPos -= clipToPlay.samples;
                var shortFrame = VoiceChatUtils.FloatToShort(frame);
                _audioProcessor.ProcessFrame(shortFrame);
                _networkModule.SendFrame(shortFrame);
                yield return new WaitForSecondsRealtime(0.0134f);
            }
        }
    }
}