using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    /// <summary>
    /// Class to manage a webcamera connection
    /// </summary>
    public class WebCamera
    {
        /// <summary>
        /// The device used
        /// </summary>
        private readonly WebCamDevice _camDevice;
        
        /// <summary>
        /// The cam texture handling the webcamera connection
        /// </summary>
        private WebCamTexture _camTexture;
        
        /// <summary>
        /// When we capture a frame from the camera, this is where we store the results.
        /// </summary>
        private Texture2D _texture;
        
        /// <summary>
        /// This is the available resolutions for the camera.
        /// </summary>
        private readonly List<(int, int)> _availableResolutions = new List<(int, int)>();
        
        /// <summary>
        /// Keeps track of the previous frames update count from <see cref="_camTexture"/>.
        /// If its different that means it has been updated.
        /// </summary>
        private uint _updateCounter = 0;


        /// <summary>
        /// Constructor for the camera.
        /// </summary>
        /// <param name="device">The device to connect to.</param>
        public WebCamera(WebCamDevice device)
        {
            _camDevice = device;
        }

        /// <summary>
        /// This will connect and start the camera. This needs to be run in a coroutine.
        /// </summary>
        /// <param name="resolution">This is the resolution of the camera starting at 0 as the max resolution.</param>
        /// <returns>IEnumerator for the coroutine.</returns>
        public IEnumerator Start(int resolution, int requestedFPS = 30)
        {
            if (_camTexture != null) _camTexture.Stop();
            _camTexture = new WebCamTexture(_camDevice.name, 1280, 720);
            _camTexture.Play();
            while (_camTexture.width <= 16) yield return null;
            FindAvailableResolutions(_camTexture.width, _camTexture.height);
            _camTexture.Stop();
            _camTexture = new WebCamTexture(_camDevice.name, _availableResolutions[resolution].Item1, _availableResolutions[resolution].Item2, requestedFPS);
            _camTexture.Play();
            while (_camTexture.width <= 16) yield return null;
            _texture = new Texture2D(_camTexture.width, _camTexture.height, TextureFormat.RGBA32, false);
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Started camera: " + _camTexture.deviceName + " " + _camTexture.width + "x" + _camTexture.height);
        }

        /// <summary>
        /// Start streaming from this camera to the given network module. Using jpg encoding.
        /// </summary>
        /// <param name="networkModule">The network module to stream the frames to.</param>
        /// <param name="jpgQuality">The quality of the jpg encoding.</param>
        /// <returns>IEnumerable for a coroutine.</returns>
        public IEnumerable StartJpgStreaming(VideoStreamNetworkModule networkModule, int jpgQuality = 75)
        {
            while (true)
            {
                if (!HasUpdated()) yield return null;
                networkModule.SendFrame(GetJpg(jpgQuality));
            }
        }

        /// <summary>
        /// There is a new frame available.
        /// </summary>
        /// <returns>Whether there is new frame available or not.</returns>
        public bool HasUpdated()
        {
            return _updateCounter != _camTexture.updateCount;
        }

        /// <summary>
        /// Get the newest frame, in jpg format.
        /// </summary>
        /// <param name="quality">The quality of the jpg compression, from  1-100.</param>
        /// <returns>The jpg image.</returns>
        public byte[] GetJpg(int quality = 75)
        {
            _texture.SetPixels32(_camTexture.GetPixels32());
            _updateCounter = _camTexture.updateCount;
            return _texture.EncodeToJPG(quality);
        }
        
        /// <summary>
        /// Given a max resolution, find the available resolutions for this camera.
        /// The results are added to <see cref="_availableResolutions"/>.
        /// </summary>
        /// <param name="maxWidth">The max with of the camera.</param>
        /// <param name="maxHeight">The max height of the camera.</param>
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