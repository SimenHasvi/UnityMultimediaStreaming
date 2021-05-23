using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOutput2 : MonoBehaviour
{
    [Header("The User ID to Listen For")]
    public int id;

    private AudioSource _audioSource;
    private Queue<float[]> _frameBuffer = new Queue<float[]>();
    private int _framesInAudioSource = 50;
    private bool _newAudio = false;
    private int _latestPos = -1;
    private int _endOfData = 0;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        VoiceChatUtils.OnNewFrame += OnNewFrame;
        VoiceChatUtils.idsListenedTo.Add(id);
        Debug.Log("Playing audio from user: " + id);
        StartCoroutine(PlayFrameBuffer());
    }

    private IEnumerator PlayFrameBuffer()
    {
        while (VoiceChatUtils.sampleRateStatic < 0 && VoiceChatUtils.frameSizeStatic < 0) yield return null;
        _audioSource.clip = AudioClip.Create("voice_chat_clip", VoiceChatUtils.frameSizeStatic * _framesInAudioSource, 1, VoiceChatUtils.sampleRateStatic, false);
        _audioSource.loop = true;
        _audioSource.Play();
        while (true)
        {
            while (_frameBuffer.Count <= 0) yield return null;
            _audioSource.clip.SetData(_frameBuffer.Dequeue(), _endOfData);
            _endOfData += VoiceChatUtils.frameSizeStatic;
            if (_endOfData >= _audioSource.clip.samples) _endOfData = 0;
        }
    }

    private void OnNewFrame(int headerId, float[] newFrame)
    {
        if (headerId != id) return;
        _frameBuffer.Enqueue(newFrame);
    }
}