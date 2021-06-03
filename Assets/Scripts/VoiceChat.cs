using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AudioProcessingModuleCs.Media;
using AudioProcessingModuleCs.Media.Dsp;
using AudioProcessingModuleCs.Media.Dsp.WebRtc;
using Concentus.Enums;
using Concentus.Structs;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : MonoBehaviour
{
    [Header("Debug stuff")]
    public bool verbose;
    [Range(10, 100)]
    public int targetFramerate;
    public bool playBackSelf;
    
    [Header("Audio Format")]
    public int sampleRate;
    public int millisecondsPerFrame;
    
    [Header("Audio Compression")]
    public OpusApplication compressionMode = OpusApplication.OPUS_APPLICATION_VOIP;
    public int bitrate;
    
    [Header("Audio Enhancments")]
    public bool acousticEchoCancellation;
    public bool noiseSuppression;
    public bool automaticGainControl;
    public int expectedDelay;
    public int filterLength;
    
    [Header("Networking")] 
    public int id;
    public string serverUri;
    public string serverTopic;
    
    // sampling and playback
    private readonly Dictionary<int, Queue<short[]>> _frameBuffers = new Dictionary<int, Queue<short[]>>();
    private int _lastPos, _pos = 0;
    private int _endOfData = 0;
    private AudioClip _mic;
    private AudioSource _audioSource;
    private const int FramesInAudioSource = 50;
    
    // audio handling
    private AudioFormat _audioFormat;
    private static OpusEncoder _encoder;
    private static OpusDecoder _decoder;
    private static WebRtcFilter _enhancer;
    
    // network
    private static Producer _producer;
    private static Consumer _consumer;
    private static Thread _consumeThread;

    private void Start()
    {
        // set target framerate
        Application.targetFrameRate = targetFramerate;
        Debug.Log("target framerate set to: " + targetFramerate);
        
        // start networking
        var options = new ConsumerOptions(serverTopic, new BrokerRouter(new KafkaOptions(new Uri(serverUri))));
        options.MinimumBytes = 1;
        _consumer = new Consumer(options);
        var offsets = _consumer.GetTopicOffsetAsync(serverTopic).Result
            .Select(x => new OffsetPosition(x.PartitionId, x.Offsets.Max())).ToArray();
        _consumer = new Consumer(options, offsets);
        _consumeThread = new Thread(Consume);
        _consumeThread.Start();
        _producer = new Producer(new BrokerRouter(new KafkaOptions(new Uri(serverUri))));
        
        // set up encoder/decoder/enchacer
        _audioFormat = new AudioFormat(sampleRate, millisecondsPerFrame, 1, sizeof(short) * 8);
        _encoder = new OpusEncoder(_audioFormat.SamplesPerSecond, _audioFormat.Channels, compressionMode) {Bitrate = bitrate, UseVBR = true, SignalType = OpusSignal.OPUS_SIGNAL_VOICE, ForceMode = OpusMode.MODE_SILK_ONLY};
        _decoder = new OpusDecoder(_audioFormat.SamplesPerSecond, _audioFormat.Channels);
        var resampleFilter = new ResampleFilter(_audioFormat, _audioFormat);
        _enhancer = new WebRtcFilter(expectedDelay, filterLength, _audioFormat, _audioFormat, acousticEchoCancellation, noiseSuppression, automaticGainControl, resampleFilter);
        Debug.Log("encoder sample rate: " + _encoder.SampleRate + ", channels: " + _encoder.NumChannels + ", compression mode: " + _encoder.Application + ", bitrate: " + _encoder.Bitrate);
        Debug.Log("decoder sample rate: " + _decoder.SampleRate + ", channels: " + _decoder.NumChannels);

        _audioSource = GetComponent<AudioSource>();
        _mic = Microphone.Start(Microphone.devices[0], true, 50, _audioFormat.SamplesPerSecond);
        
        StartCoroutine(SampleAudio());
        StartCoroutine(PlayAudio());
    }

    private void Produce(short[] frame)
    {
        _enhancer.Write(ToByteStream(frame));
        if (_enhancer.Read(frame, out _)) _producer.SendMessageAsync(serverTopic, new[] {new Message(Encode(id, frame))});
    }
    
    void Consume()
    {
        Debug.Log("starting consumer with url: " + serverUri + ", topic: " + serverTopic + ", " + _consumer.GetOffsetPosition()[0]);
        foreach (var message in _consumer.Consume())
        {
            var (headerId, audioClip) = Decode(message.Value);
            AddFrameToBuffer(headerId, audioClip);
        }
    }
    
    private byte[] Encode(int id, short[] frame)
    {
        var compressedFrame = new byte[frame.Length];
        var len = _encoder.Encode(frame, 0, frame.Length, compressedFrame, 0, frame.Length);
        Array.Resize(ref compressedFrame, len);
        var packet = new byte[compressedFrame.Length + 1];
        packet[0] = (byte)id;
        Array.Copy(compressedFrame, 0, packet, 1, compressedFrame.Length);
        return packet;
    }

    private (int, short[]) Decode(byte[] packet)
    {
        var id = (int)packet[0];
        var frame = new short[OpusPacketInfo.GetNumSamples(packet, 1, packet.Length-1, _audioFormat.SamplesPerSecond)];
        _decoder.Decode(packet, 1, packet.Length-1, frame, 0, frame.Length);
        return (id, frame);
    }

    void AddFrameToBuffer(int headerId, short[] frame)
    {
        if (playBackSelf && headerId == id) return;
        if (!_frameBuffers.ContainsKey(headerId)) _frameBuffers.Add(headerId, new Queue<short[]>());
        while (_frameBuffers[headerId].Count > _audioFormat.FramesPerSecond / 4) _frameBuffers[headerId].Dequeue();
        _frameBuffers[headerId].Enqueue(frame);
    }

    short[] GetNextFrameFromBuffer()
    {
        var combinedFrame = new short[_audioFormat.SamplesPerFrame];
        foreach (var frameBuffer in _frameBuffers.Values)
        {
            if (frameBuffer.Count <= 0) continue;
            var frame = frameBuffer.Dequeue();
            for (var i = 0; i < combinedFrame.Length; i++)
            {
                combinedFrame[i] += frame[i];
            }
        }
        _enhancer.RegisterFramePlayed(ToByteStream(combinedFrame));
        return combinedFrame;
    }
    
    private IEnumerator SampleAudio()
    {
        var frame = new float[_audioFormat.SamplesPerFrame];
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
            Produce(FloatToShort(frame));
        }
    }
    
    private IEnumerator PlayAudio()
    {
        _audioSource.clip = AudioClip.Create("voice_chat_clip", _audioFormat.SamplesPerFrame * FramesInAudioSource, 1, _audioFormat.SamplesPerSecond, false);
        _audioSource.loop = true;
        _audioSource.Play();
        while (true)
        {
            _audioSource.clip.SetData(ShortToFloat(GetNextFrameFromBuffer()), _endOfData);
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
}
