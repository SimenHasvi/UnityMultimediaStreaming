using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    public class VideoStream : MonoBehaviour
    {
        [Header("Network:")]
        public int id;
        public string serverUri;
        public string serverTopic;
        public bool localNetwork = true;
        [Range(1, 100)]
        public int compressionQuality = 50;

        [Header("WebCamera")]
        [Range(0, 10)]
        public int resolution;
        [Range(1, 60)]
        public int requestedFPS;

        private Dictionary<int, Texture2D> _textures = new Dictionary<int, Texture2D>();
        private VideoStreamNetworkModule _videoStreamNetworkModule;
        private VideoFrameBuffer _videoFrameBuffer;
        private WebCamera _webCamera;

        private void Start()
        {
            VoiceChatUtils.EnableUnityLogging(true);
            _videoFrameBuffer = new Texture2DjpgVideoFrameBuffer();
            if (localNetwork) _videoStreamNetworkModule = new LocalVideoStreamNetworkModule();
            else _videoStreamNetworkModule = new KafkaVideoStreamNetworkModule(id, serverUri, serverTopic);
            _videoStreamNetworkModule.StartListenForFrames(_videoFrameBuffer);

            _webCamera = new WebCamera(WebCamTexture.devices[0]);
            StartCoroutine(_webCamera.Start(1, requestedFPS));
        }

        private void Update()
        {
            CheckForNewUsers();
            foreach (var texture in _textures)
            {
                _videoFrameBuffer.GetNextFrameFromBuffer(texture.Key, texture.Value);
            }
            if (_webCamera.HasUpdated()) _videoStreamNetworkModule.SendFrame(_webCamera.GetJpg(compressionQuality));
        }

        private void CheckForNewUsers()
        {
            foreach (var newUserId in _videoFrameBuffer.GetActiveBufferIds().Except(_textures.Keys))
            {
                _textures.Add(newUserId, new Texture2D(0, 0));
                var videoObject = new GameObject("Video" + id);
                videoObject.transform.parent = gameObject.transform;
                videoObject.AddComponent<RawImage>();
                videoObject.GetComponent<RawImage>().texture = _textures[newUserId];
            }
        }
    }
}