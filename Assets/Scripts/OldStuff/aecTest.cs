using System;
using System.Collections;
using System.Collections.Generic;
using AudioProcessingModuleCs.Media;
using AudioProcessingModuleCs.Media.Dsp;
using AudioProcessingModuleCs.Media.Dsp.WebRtc;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class aecTest : MonoBehaviour
{
    public int sampleRate;
    public int millisecondsPerFrame;
    public bool acousticEchoCancellation;
    public bool noiseSuppression;
    public bool automaticGainControl;
    public int expectedDelay;
    public int filterLength;
    
    private static WebRtcFilter _enhancer;
    
    private int _lastPos, _pos = 0;
    private AudioClip _mic;
    
    private AudioSource _audioSource;
    private AudioFormat _audioFormat;
    private readonly Queue<short[]> _frameBuffer = new Queue<short[]>();
    private const int FramesInAudioSource = 50;
    private int _endOfData = 0;

    private void Start()
    {
        _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame, 1, sizeof(short) * 8);
        var resampleFilter = new ResampleFilter(_audioFormat, _audioFormat);
        _enhancer = new WebRtcFilter(expectedDelay, filterLength, _audioFormat, _audioFormat, acousticEchoCancellation, noiseSuppression, automaticGainControl, resampleFilter);
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
            while (_frameBuffer.Count > _audioFormat.FramesPerSecond / 4) _frameBuffer.Dequeue();
            _enhancer.Write(ToByteStream(FloatToShort(frame)));
            var cancelBuffer = new short[_audioFormat.SamplesPerFrame];
            if (_enhancer.Read(cancelBuffer, out _)) _frameBuffer.Enqueue(cancelBuffer);
        }
    }
    
    private IEnumerator PlayAudio()
    {
        _audioSource.clip = AudioClip.Create("voice_chat_clip", _audioFormat.SamplesPerFrame * FramesInAudioSource, 1, _audioFormat.SamplesPerSecond, false);
        _audioSource.loop = true;
        _audioSource.Play();
        while (true)
        {
            if (_frameBuffer.Count > 0)
            {
                var frame = _frameBuffer.Dequeue();
                _enhancer.RegisterFramePlayed(ToByteStream(frame));
                _audioSource.clip.SetData(ShortToFloat(frame), _endOfData);
            }
            else _audioSource.clip.SetData(new float[_audioFormat.SamplesPerFrame], _endOfData);
            _endOfData += _audioFormat.SamplesPerFrame;
            if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
            while (CircularDistanceTo(_audioSource.timeSamples, _endOfData, _audioSource.clip.samples) > _audioFormat.SamplesPerFrame * 10) yield return null;
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

    private byte[] ToByteStream(short[] shorts)
    {
        var bytes = new byte[shorts.Length * sizeof(short)];
        Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private short[] ByteStreamToShort(byte[] bytes)
    {
        var shorts = new short[bytes.Length / sizeof(short)];
        Buffer.BlockCopy(bytes, 0, shorts, 0, bytes.Length);
        return shorts;
    }

}
