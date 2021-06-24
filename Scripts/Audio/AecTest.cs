using System.Collections;
using UnityEngine;

namespace VoiceChat
{
    public class AecTest : MonoBehaviour
    {
        [Header("Here you can play around with various settings")]
        public bool aec = false;
        public int aecFilterLengthMs = 100;
        public bool deNoise = false;
        public bool agc = false;
        public bool vad = false;
        public float agcLevel = 0;
        public bool deReverb = false;
        public float deReverbLevel = 0;
        public float deReverbDecay = 0;
        public int vadProbStart = 35;
        public int vadProbContinue = 20;
        public int noiseSuppress = -15;
        public int echoSuppress = -40;
        public int echoSuppressActive = -15;
        public int agcIncrement = 12;
        public int agcDecrement = -40;
        public int agcMaxGain = 30;
        public int agcTarget = 8000;

        private AudioFormat _audioFormat;
        private AudioFrameBuffer _frameBuffer;
        private AudioFrameBuffer _debugFrameBuffer;
        private DummyAudioCodec _audioCodec;
        private LocalVoiceChatNetworkModule _networkModule;
        private SpeexDspAudioProcessor _audioProcessor;
        private AudioPlayback _audioPlayback;
        
        private int _lastPos, _pos = 0;
        private AudioClip _mic;
        
        void Start()
        {
            VoiceChatUtils.EnableUnityLogging(true);
            _audioFormat = new AudioFormat(16000, 20);
            _audioCodec = new DummyAudioCodec(_audioFormat);
            _frameBuffer = new AudioFrameBuffer(_audioFormat);
            //_debugFrameBuffer = new AudioFrameBuffer(_audioFormat);
            //_debugFrameBuffer.SetBufferSizeMs(100000);
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, aec, aecFilterLengthMs);
            _audioProcessor.Configure
            (
                deNoise,
                agc,
                vad,
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
            _networkModule = new LocalVoiceChatNetworkModule(_audioCodec);
            _networkModule.StartListenForFrames(_audioFormat, _frameBuffer);
            gameObject.AddComponent<AudioPlayback>();
            _audioPlayback = GetComponent<AudioPlayback>();
            _audioPlayback.Play(_audioFormat, _frameBuffer, _audioProcessor);
            //_audioPlayback.Play(_audioFormat, _frameBuffer);
            StartCoroutine(SampleAudio());
        }
        
        private IEnumerator SampleAudio()
        {
            _mic = Microphone.Start(Microphone.devices[0], true, 50, _audioFormat.SamplingRate);
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
                //if (Input.GetKeyDown(KeyCode.Space)) break;
            }
            //_debugFrameBuffer.DumpBuffers(Application.streamingAssetsPath);
            //Debug.Log("Debug audio dumped in " + Application.streamingAssetsPath);
            //Application.Quit();
        }
    }
}