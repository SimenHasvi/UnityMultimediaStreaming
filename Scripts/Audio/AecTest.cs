using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityVoiceChat.Scripts.Audio;

namespace VoiceChat
{
    public class AecTest : MonoBehaviour
    {
        private AudioFormat _audioFormat;
        private AudioFrameBuffer _audioFrameBuffer;
        private AudioFrameBuffer _debugAudioFrameBuffer;
        private AudioCodec _audioCodec;
        private AudioProcessor _audioProcessor;
        private VoiceChatNetworkModule _networkModule;
        private AudioPlayback _audioPlayback;
        private AudioClip _mic;
        private int _lastPos, _pos = 0;
        private IntPtr aecState;
        private AudioSource _audioSource;
        
        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.clip = Microphone.Start(null, true, 10, 44100);
            _audioSource.loop = true;
            while (Microphone.GetPosition(null) <= 0) {}
            _audioSource.Play();
        }
    }
}