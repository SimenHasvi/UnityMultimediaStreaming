using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Concentus.Common;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using NSpeex;
using UnityEngine;
using Concentus.Enums;
using Concentus.Structs;
using Microsoft.Win32;
using Debug = UnityEngine.Debug;

public class VoiceChatUtils : MonoBehaviour
{
    [Header("Enable verbose mode:")]
    public bool verbose;

    [Header("MicInputObject")] 
    public MicInput micInput;
    
    [Header("Limit FPS")]
    [Range(10, 100)]
    public int targetFramerate;

    [Header("Audio Parameters")] 
    public int sampleRate;
    public int frameSize;
    public OpusApplication compressionMode = OpusApplication.OPUS_APPLICATION_VOIP;
    public int bitrate;

    [Header("Kafka Server")] 
    public int id;
    public bool localNetworking;
    public string serverUri;
    public string serverTopic;

    public delegate void NewFrameDelegate(int id, float[] frame);
    public static NewFrameDelegate OnNewFrame;
    public static List<int> idsListenedTo = new List<int>();

    private static OpusEncoder _encoder;
    private static OpusDecoder _decoder;
    private static Producer _producer;
    private static Consumer _consumer;
    private static Thread _consumeThread;
    private static string _serverTopicStatic;
    private static int _idStatic;

    public static bool verboseStatic;
    public static int frameSizeStatic = -1;
    public static int sampleRateStatic = -1;

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

        // set up static variables
        _serverTopicStatic = serverTopic;
        _idStatic = id;
        sampleRateStatic = sampleRate;
        frameSizeStatic = frameSize;
        verboseStatic = verbose;

        // set up encoder/decoder
        _encoder = new OpusEncoder(sampleRate, 1, compressionMode) {Bitrate = bitrate};
        _decoder = new OpusDecoder(sampleRate, 1);
        Debug.Log("encoder sample rate: " + _encoder.SampleRate + ", channels: " + _encoder.NumChannels + ", compression mode: " + _encoder.Application + ", bitrate: " + _encoder.Bitrate);
        Debug.Log("decoder sample rate: " + _decoder.SampleRate + ", channels: " + _decoder.NumChannels);

        // start sampling from microphone
        micInput.Initialize(sampleRate, frameSize);
    }
    
    void Consume()
    {
        Debug.Log("starting consumer with url: " + serverUri + ", topic: " + serverTopic + ", " + _consumer.GetOffsetPosition()[0]);
        foreach (var message in _consumer.Consume())
        {
            var (headerId, audioClip) = Decode(message.Value);
            OnNewFrame.Invoke(headerId, audioClip);
        }
    }

    public static void Produce(float[] audioClip)
    {
        _producer.SendMessageAsync(_serverTopicStatic, new[] {new Message(Encode(_idStatic, audioClip))});
    }
    
    private static byte[] Encode(int id, float[] frame)
    {
        var compressedFrame = new byte[frame.Length];
        var len = _encoder.Encode(frame, 0, frame.Length, compressedFrame, 0, frame.Length);
        Array.Resize(ref compressedFrame, len);
        var packet = new byte[compressedFrame.Length + 1];
        packet[0] = (byte)id;
        Array.Copy(compressedFrame, 0, packet, 1, compressedFrame.Length);
        if (verboseStatic) Debug.Log("sending packet with frame size: " + frame.Length + ", compressed bytes: " + compressedFrame.Length);
        return packet;
    }

    private static (int, float[]) Decode(byte[] packet)
    {
        var id = (int)packet[0];
        if (!idsListenedTo.Contains(id)) return (-1, new float[0]);
        var frame = new float[OpusPacketInfo.GetNumSamples(packet, 1, packet.Length-1, 48000)];
        _decoder.Decode(packet, 1, packet.Length-1, frame, 0, frame.Length);
        if (verboseStatic) Debug.Log("received packet from id: " + id + ", frame size: " + frame.Length);
        return (id, frame);
    }

    private void OnDestroy()
    {
        _consumeThread.Abort();
        _consumeThread.Join();
    }
    
    private void OnDisable()
    {
        _consumeThread.Abort();
        _consumeThread.Join();
    }
}
