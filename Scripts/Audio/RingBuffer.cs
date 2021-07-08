using System.Collections.Generic;

namespace UnityMultimediaStreaming.Scripts.Audio
{
    
    /// <summary>
    /// NOT CURRENTLY BEING USED
    /// </summary>
    public class RingBuffer
    {
        private short[][] _frames;
        private int _newestFrame = 0;
        private int _oldestFrame = 0;

        public RingBuffer(int frames, AudioFormat audioFormat)
        {
            _frames = new short[frames][];
            for (var i = 0; i < _frames.Length; i++)
            {
                _frames[i] = new short[audioFormat.SamplesPerFrame];
            }
        }

        public ref short[] AddFrame()
        {
            _newestFrame = StepForward(_newestFrame);
            if (_oldestFrame == _newestFrame) _oldestFrame = StepForward(_oldestFrame);
            return ref _frames[_newestFrame];
        }
        
        public IEnumerable<short[]> DumpFrames()
        {
            while (StepBack(_newestFrame) != _oldestFrame)
            {
                yield return _frames[_newestFrame];
                _newestFrame = StepBack(_newestFrame);
            }
        }
        
        public IEnumerable<short[]> DumpFramesReverse()
        {
            while (_newestFrame != _oldestFrame)
            {
                yield return _frames[_oldestFrame];
                _oldestFrame = StepForward(_oldestFrame);
            }
        }

        private int StepForward(int current)
        {
            return current >= _frames.Length - 1 ? 0 : current + 1;
        }
        
        private int StepBack(int current)
        {
            return current <= 0 ? _frames.Length-1 : current - 1;
        }
    }
}