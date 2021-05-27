using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOutput3 : MonoBehaviour
{
    [Header("The User ID to Listen For")]
    public int id;

    private AudioSource _audioSource;
    private readonly Queue<float> _buffer = new Queue<float>();

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        VoiceChatUtils.OnNewFrame += OnNewFrame;
        VoiceChatUtils.idsListenedTo.Add(id);
        _audioSource.pitch = 0.5f;
        _audioSource.Play();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (_buffer.Count <= data.Length) return;
        for (var i = 0; i < data.Length; i++)
        {
            if (_buffer.Count <= 0) break;
            data[i] = _buffer.Dequeue();
        }
    }

    private void OnNewFrame(int headerId, float[] newFrame)
    {
        if (headerId != id) return;
        if (_buffer.Count > VoiceChatUtils.sampleRateStatic * 2) return;
        for (var i = newFrame.Length-1; i >= 0; i--)
        {
            _buffer.Enqueue(newFrame[i]);
        }
    }
}