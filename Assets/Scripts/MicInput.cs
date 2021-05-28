using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using KafkaNet;
using Debug = UnityEngine.Debug;

public class MicInput : MonoBehaviour
{
    
    [Header("Audio Sampling Parameters")]
    [Range(0.0f, 1.0f)]
    public float threshold;
    
    private float[] _frame;
    private int _lastPos, _pos = 0;
    private AudioClip _mic;

    public void Initialize(int frequency, int frameSize)
    {
        _frame = new float[frameSize];
        _mic = Microphone.Start(Microphone.devices[0], true, 50, frequency);
        Debug.Log("sampling from microphone: " + Microphone.devices[0] + ", frequency: " + _mic.frequency);
        StartCoroutine(SampleAudio());
    }

    private IEnumerator SampleAudio()
    {
        while (true)
        {
            while (_pos - _lastPos < _frame.Length)
            {
                _pos = Microphone.GetPosition(Microphone.devices[0]);
                if (_pos < _lastPos) _lastPos = 0;
                yield return null;
            }
            _mic.GetData(_frame, _lastPos);
            _lastPos += _frame.Length;
            if (_frame.Max() < threshold) continue;
            VoiceChatUtils.Produce(_frame);
        }
    }
}
