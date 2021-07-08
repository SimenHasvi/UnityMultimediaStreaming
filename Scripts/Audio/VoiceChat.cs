using System.Collections;
using UnityEngine;

namespace UnityMultimediaStreaming.Scripts.Audio
{
    public class VoiceChat : MonoBehaviour
    {
        [Header("Target FPS for debugging.")] 
        [Range(1, 100)]
        public int targetFPS;

        [Header("Network")]
        public bool playSelf = false;
        public int id = 0;
        public string serverUrl;
        public int roomNumber;
        private string userToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1c2VyX3VzZXJuYW1lIjoidGVhY2hlciIsInVzZXJfcGFzc3dvcmQiOiIkYXJnb24yaSR2PTE5JG09NjU1MzYsdD00LHA9MSRibFJJVjBrM05XTlZjVzF5ZFdWV1ZBJFNXbEs5NGlabW5WVm82ZkhkVlhWT2xXZjAyTE52aVltZ0dtQkNEM1E0OGcifQ.S6N4NaXkrUABjDHWw8Co2psHtc0ACg6PCPErx-RskJs";
        public bool localNetwork = true;
        
        [Header("Audio Format")]
        public int sampleRate = 16000;
        public int millisecondsPerFrame = 20;
        public int millisecondsInBuffer = 500;

        [Header("Audio Processing")]
        public bool deNoise = true;
        public bool automaticGainControl = true;
        public bool voiceActivityDetector = true;
        public bool acousticEchoCancellation = true;
        public bool deReverb = false;
        public int aecFilterLengthMs = 200;
        public float agcLevel = 0;
        public float deReverbLevel = 0;
        public float deReverbDecay = 0;
        public int vadProbStart = 99;
        public int vadProbContinue = 80;
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

        private bool _resetEncoderState = false;
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
            Application.targetFrameRate = targetFPS;
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
            else _networkModule = new KafkaVoiceChatNetworkModule(id, serverUrl, roomNumber, _audioFormat, _audioCodec);
            _networkModule.StartListenForFrames(_audioFrameBuffer);
            //StartCoroutine(((KafkaVoiceChatNetworkModule)_networkModule).MuteUser(id, userToken));
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
            var recordedFrames = new RingBuffer(10, _audioFormat);
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
                var vadResult = _audioProcessor.ProcessFrame(shortFrame, recordedFrames.AddFrame());
                if (vadResult)
                {
                    foreach (var recordedFrame in recordedFrames.DumpFramesReverse())
                    {
                        _networkModule.SendFrame(recordedFrame, _resetEncoderState);
                        _resetEncoderState = false;
                    }
                }
                else
                {
                    //since there is a gap in the sent audio signal we need to reset the encoder the next frame sent
                    if (recordedFrames.IsFull()) _resetEncoderState = true;
                }
            }
        }

        private void OnDisable()
        {
            _networkModule.StopListenForFrames();
        }
    }
}