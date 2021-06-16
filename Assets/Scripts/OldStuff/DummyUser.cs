using System;
using System.Threading;
using AudioProcessingModuleCs.Media;
using Concentus.Enums;
using Concentus.Structs;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using UnityEngine;

public class DummyUser : MonoBehaviour
{
    [Header("Audio to loop")]
    public AudioClip _clip;
    
    [Header("Audio Format")]
    public int millisecondsPerFrame;

    [Header("Audio Compression")] 
    [Range(0, 10)]
    public int complexity;
    public OpusApplication compressionMode = OpusApplication.OPUS_APPLICATION_VOIP;
    public int bitrate;

    [Header("Networking")] 
    public int id;
    public string serverUri;
    public string serverTopic;
    
    // sampling and playback
    private int _lastPos, _pos = 0;
    private Thread _thread;

    // audio handling
    private AudioFormat _audioFormat;
    private static OpusEncoder _encoder;
    private float[] _samples;

    // network
    private static Producer _producer;

    private void Start()
    {
        Debug.Log("Dummy user looping clip with sample rate: " + _clip.frequency + " and frame size: " + millisecondsPerFrame + "ms.");
        _producer = new Producer(new BrokerRouter(new KafkaOptions(new Uri(serverUri))));
        _audioFormat = new AudioFormat(_clip.frequency, millisecondsPerFrame, 1, sizeof(short) * 8);
        _encoder = new OpusEncoder(_audioFormat.SamplesPerSecond, _audioFormat.Channels, compressionMode) {Bitrate = bitrate, UseVBR = true, SignalType = OpusSignal.OPUS_SIGNAL_VOICE, ForceMode = OpusMode.MODE_SILK_ONLY, Complexity = complexity};
        _samples = new float[_clip.samples];
        _clip.GetData(_samples, 0);
        _thread = new Thread(SampleAudio);
        _thread.Start();
    }
    
    private void SampleAudio()
    {
        var frame = new float[_audioFormat.SamplesPerFrame];
        while (true)
        {
            Array.Copy(_samples, _lastPos, frame, 0, frame.Length);
            _lastPos += frame.Length;
            if (_lastPos >= _samples.Length) _lastPos -= _samples.Length;
            _producer.SendMessageAsync(serverTopic, new[] {new Message(Encode(id, FloatToShort(frame)))});
            Thread.Sleep(_audioFormat.MillisecondsPerFrame);
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

    private short[] FloatToShort(float[] floats)
    {
        var shorts = new short[floats.Length];
        for (var i = 0; i < floats.Length; i++)
        {
            shorts[i] = (short)(floats[i] * short.MaxValue);
        }
        return shorts;
    }

    private void OnDisable()
    {
        _thread.Abort();
        _thread.Join();
    }
}
