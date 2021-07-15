using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityMultimediaStreaming.Scripts.Audio
{
    public class AecTest : MonoBehaviour
    {
        private AudioFormat _audioFormat;
        private AudioProcessor _audioProcessor;
        private AudioProcessor _audioProcessorNoAEC;
        
        public void Start()
        {
            _audioFormat = new AudioFormat(16000, 20);
            _audioProcessor = new SpeexDspAudioProcessor(_audioFormat, true, 200);
            _audioProcessorNoAEC = new SpeexDspAudioProcessor(_audioFormat, false);
            StartCoroutine(SampleAudio());
        }

        private IEnumerator SampleAudio()
        {
            var samples = new List<short>();
            var samplesNoAEC = new List<short>();
            
            var echoFrames = new Queue<short[]>();
            var echoFramesNoAEC = new Queue<short[]>();
            
            for (var i = 0; i < _audioFormat.FramesPerSecond; i++) echoFrames.Enqueue(new short[_audioFormat.SamplesPerFrame]);
            for (var i = 0; i < _audioFormat.FramesPerSecond; i++) echoFramesNoAEC.Enqueue(new short[_audioFormat.SamplesPerFrame]);
            
            var lastPos = 0;
            var pos = 0;
            var mic = Microphone.Start(Microphone.devices[0], true, 50, _audioFormat.SamplingRate);
            var numberOfFrames = 0;
            while (true)
            {
                var tmp = new float[_audioFormat.SamplesPerFrame];
                while (pos - lastPos < tmp.Length)
                {
                    pos = Microphone.GetPosition(Microphone.devices[0]);
                    if (pos < lastPos) lastPos = 0;
                    yield return null;
                }
                mic.GetData(tmp, lastPos);
                lastPos += tmp.Length;
                
                var frame = new short[_audioFormat.SamplesPerFrame];
                VoiceChatUtils.FloatToShort(frame, tmp);
                var echoFrame = echoFrames.Dequeue();
                for (var i = 0; i < frame.Length; i++) frame[i] += echoFrame[i];
                var processedFrame = new short[_audioFormat.SamplesPerFrame];
                _audioProcessor.ProcessFrame(frame, echoFrame, processedFrame);
                echoFrames.Enqueue(processedFrame);
                samples.AddRange(processedFrame);
                
                frame = new short[_audioFormat.SamplesPerFrame];
                VoiceChatUtils.FloatToShort(frame, tmp);
                echoFrame = echoFramesNoAEC.Dequeue();
                for (var i = 0; i < frame.Length; i++) frame[i] += echoFrame[i];
                processedFrame = new short[_audioFormat.SamplesPerFrame];
                _audioProcessorNoAEC.ProcessFrame(frame, echoFrame, processedFrame);
                echoFramesNoAEC.Enqueue(processedFrame);
                samplesNoAEC.AddRange(processedFrame);
                
                if (++numberOfFrames == _audioFormat.FramesPerSecond * 20) break;
            }
            SaveWav.Save(Path.Combine(Application.streamingAssetsPath, "aec"), _audioFormat, VoiceChatUtils.ShortToFloat(samples.ToArray()));
            SaveWav.Save(Path.Combine(Application.streamingAssetsPath, "noAec"), _audioFormat, VoiceChatUtils.ShortToFloat(samplesNoAEC.ToArray()));
        }
    }
}