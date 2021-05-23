using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioOutput : MonoBehaviour
{
    [Header("The User ID to Listen For")]
    public int id;

    private AudioSource _audioSource;
    private float[] _audioData = {0f};
    private bool _newClip = false;
    private bool _audioSourceInitialized = false;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayAudio());
        VoiceChatUtils.OnNewFrame += OnNewFrame;
        VoiceChatUtils.idsListenedTo.Add(id);
        Debug.Log("Playing audio from user: " + id);
    }

    IEnumerator PlayAudio()
    {
        while (VoiceChatUtils.sampleRateStatic < 0 && VoiceChatUtils.frameSizeStatic < 0) yield return null;
        _audioSource.clip = AudioClip.Create("voice_chat_clip", VoiceChatUtils.frameSizeStatic, 1, VoiceChatUtils.sampleRateStatic, false);
        while (true)
        {
            while (!_newClip || _audioSource.isPlaying) yield return null;
            _newClip = false;
            _audioSource.clip.SetData(_audioData, 0);
            _audioSource.Play();
        }
    }

    private void OnNewFrame(int headerId, float[] newAudioData)
    {
        if (headerId != id) return;
        _audioData = newAudioData;
        _newClip = true;
    }
}
