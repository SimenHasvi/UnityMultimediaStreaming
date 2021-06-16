using System;
using System.Collections;
using System.Collections.Generic;
using AudioProcessingModuleCs.Media;
using AudioProcessingModuleCs.Media.Dsp;
using AudioProcessingModuleCs.Media.Dsp.WebRtc;
using UnityEngine;
using Version3;
using AudioFormat = Version3.AudioFormat;

[RequireComponent(typeof(AudioSource))]
public class SpeexTest : MonoBehaviour
{
    public int sampleRate;
    public int millisecondsPerFrame;
    public int millisecondsInBuffer;
    public bool denoise;
    public bool automaticGainControl;
    public bool voiceActivityDetector;
    public bool acousticEchoCancellation;

    private int _lastPos, _pos = 0;
    private AudioClip _mic;

    private AudioFrameBuffer _audioFrameBuffer;
    private AudioFormat _audioFormat;
    private AudioProcessor _processor;
    private AudioSource _audioSource;
    private const int FramesInAudioSource = 50;
    private int _endOfData = 0;

    private void Start()
    {
        _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame);
        _audioFrameBuffer = new AudioFrameBuffer(_audioFormat);
        _audioFrameBuffer.SetBufferSizeMs(millisecondsInBuffer);
        _processor = new SpeexDspAudioProcessor(_audioFormat, denoise, automaticGainControl, voiceActivityDetector, acousticEchoCancellation);
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
            var shortFrame = FloatToShort(frame);
            _processor.ProcessFrame(shortFrame);
            _audioFrameBuffer.AddFrameToBuffer(shortFrame);
        }
    }
    
    private IEnumerator PlayAudio()
    {
        _audioSource.clip = AudioClip.Create("voice_chat_clip", _audioFormat.SamplesPerFrame * FramesInAudioSource, 1, _audioFormat.SamplingRate, false);
        _audioSource.loop = true;
        _audioSource.Play();
        while (true)
        {
            var frame = _audioFrameBuffer.GetNextFrameFromBuffer();
            _processor.RegisterPlayedFrame(frame);
            _audioSource.clip.SetData(ShortToFloat(frame), _endOfData);
            _endOfData += _audioFormat.SamplesPerFrame;
            if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
            while (CircularDistanceTo(_audioSource.timeSamples, _endOfData, _audioSource.clip.samples) > _audioFormat.SamplesPerFrame * 5) yield return null;
        }
    }
    
    private int CircularDistanceTo(int from, int to, int circumference)
    {
        if (to >= from)
        {
            return to - from;
        }
        return circumference - (from - to);
    }
    
    private short[] FloatToShort(float[] floats)
    {
        var shorts = new short[floats.Length];
        for (var i = 0; i < floats.Length; i++)
        {
            shorts[i] = (short)(floats[i] * short.MaxValue);
        }
        return shorts;
    }

    private float[] ShortToFloat(short[] shorts)
    {
        var floats = new float[shorts.Length];
        for (var i = 0; i < shorts.Length; i++)
        {
            floats[i] = shorts[i] / (float)short.MaxValue;
        }
        return floats;
    }
}
