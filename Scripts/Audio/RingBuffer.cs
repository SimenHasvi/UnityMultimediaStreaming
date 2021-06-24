using System.Collections.Generic;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Audio
{
    
    /// <summary>
    /// NOT CURRENTLY BEING USED
    /// </summary>
    public class RingBuffer
    {
        private short[] _samples;
        private int _writeCounter = 0;
        private int _readCounter = 0;
        private AudioFormat _audioFormat;
        
        public RingBuffer(AudioFormat audioFormat, int sizeMs)
        {
            _audioFormat = audioFormat;
            _samples = new short[_audioFormat.SamplesInMs(sizeMs)];
        }

        public int Length()
        {
            return _writeCounter - _readCounter;
        }

        public void AddSample(short sample)
        {
            NormalizeCounters();
            _samples[_writeCounter++ % _samples.Length] = sample;
        }
        
        public void AddSample(float sample)
        {
            AddSample((short)(sample * short.MaxValue));
        }
        
        public void AddSamples(params short[] samples)
        {
            foreach (var sample in samples)
            {
                AddSample(sample);
            }            
        }
        
        public void AddSamples(params float[] samples)
        {
            foreach (var sample in samples)
            {
                AddSample(sample);
            }            
        }

        public short GetSample()
        {
            NormalizeCounters();
            if (_readCounter >= _writeCounter) return -1;
            return _samples[_readCounter++ % _samples.Length];
        }

        public float GetSampleFloat()
        {
            return GetSample() / (float) short.MaxValue;
        }

        public IEnumerable<short> GetSamples()
        {
            while (true)
            {
                yield return GetSample();
            }
        }
        
        public IEnumerable<float> GetSamplesFloat()
        {
            while (true)
            {
                yield return GetSampleFloat();
            }
        }

        private void NormalizeCounters()
        {
            if (_readCounter < _samples.Length || _writeCounter < _samples.Length) return;
            var factor = _readCounter / _samples.Length;
            _readCounter -= factor;
            _writeCounter -= factor;
        }
    }
}