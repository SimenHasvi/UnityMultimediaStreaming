using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOutput2 : MonoBehaviour
{
    [Header("The User ID to Listen For")]
    public int id;

    private AudioSource _audioSource;
    private readonly Queue<float[]> _frameBuffer = new Queue<float[]>();
    private const int FramesInAudioSource = 5;
    private int _endOfData = 0;
    private int _lastPos = 0;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        VoiceChatUtils.OnNewFrame += OnNewFrame;
        VoiceChatUtils.idsListenedTo.Add(id);
        StartCoroutine(AddFramesFromBuffer());
    }

    private IEnumerator AddFramesFromBuffer()
    {
        while (VoiceChatUtils.sampleRateStatic < 0 && VoiceChatUtils.frameSizeStatic < 0) yield return null;
        _audioSource.clip = AudioClip.Create("voice_chat_clip", VoiceChatUtils.frameSizeStatic * FramesInAudioSource, 1, VoiceChatUtils.sampleRateStatic, false);
        _audioSource.loop = true;
        Debug.Log("Playing audio from user: " + id);
        _audioSource.Play();
        while (true)
        {
            if (_frameBuffer.Count > 0) _audioSource.clip.SetData(_frameBuffer.Dequeue(), _endOfData);
            else _audioSource.clip.SetData(new float[VoiceChatUtils.frameSizeStatic], _endOfData);
            _endOfData += VoiceChatUtils.frameSizeStatic;
            if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
            while (CircularDistanceTo(_audioSource.timeSamples, _endOfData, _audioSource.clip.samples) > VoiceChatUtils.frameSizeStatic * 2) yield return null;
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

    private void OnNewFrame(int headerId, float[] newFrame)
    {
        if (headerId != id) return;
        if (_frameBuffer.Count > 50) return;
        _frameBuffer.Enqueue(newFrame);
    }
}