using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    public class WebCamera
    {
        private readonly WebCamDevice _camDevice;
        private WebCamTexture _camTexture;
        private Texture2D _texture;
        private readonly List<(int, int)> _availableResolutions = new List<(int, int)>();
        private uint _updateCounter = 0;

        public WebCamera(int camIndex)
        {
            _camDevice = WebCamTexture.devices[camIndex];
        }

        public IEnumerator Start(int quality)
        {
            if (_camTexture != null) _camTexture.Stop();
            _camTexture = new WebCamTexture(_camDevice.name, 1280, 720);
            _camTexture.Play();
            while (_camTexture.width <= 16) yield return null;
            FindAvailableResolutions(_camTexture.width, _camTexture.height);
            _camTexture.Stop();
            _camTexture = new WebCamTexture(_camDevice.name, _availableResolutions[quality].Item1, _availableResolutions[quality].Item2);
            _camTexture.Play();
            while (_camTexture.width <= 16) yield return null;
            _texture = new Texture2D(_camTexture.width, _camTexture.height, TextureFormat.RGBA32, false);
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Started camera: " + _camTexture.deviceName + " " + _camTexture.width + "x" + _camTexture.height);
        }

        public bool HasUpdated()
        {
            return _updateCounter != _camTexture.updateCount;
        }

        public byte[] GetJpg(int quality = 75)
        {
            _texture.SetPixels32(_camTexture.GetPixels32());
            _updateCounter = _camTexture.updateCount;
            return _texture.EncodeToJPG(quality);
        }
        
        private void FindAvailableResolutions(int maxWidth, int maxHeight)
        {
            _availableResolutions.Add((maxWidth, maxHeight));
            for (var i = 2; i < 10; i++)
            {
                if (i % 2 != 0 || maxWidth % i != 0 || maxHeight % i != 0) continue;
                _availableResolutions.Add((maxWidth/i, maxHeight/i));
            }
        }
    }
}