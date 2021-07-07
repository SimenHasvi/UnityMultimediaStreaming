using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace VoiceChat
{
    /// <summary>
    /// Networking module using kafka as the backend
    /// </summary>
    public class KafkaVoiceChatNetworkModule : VoiceChatNetworkModule
    {
        private readonly string _serverKafkaUri;
        private readonly string _serverRoomControlUri;
        private readonly int _roomNr;
        private readonly string _serverTopicAudioData;
        private readonly string _serverTopicController;
        private readonly Consumer _audioDataConsumer;
        private readonly Consumer _controllerConsumer;
        private readonly Producer _producer;
        private readonly Thread _consumeAudioDataThread;
        private readonly Thread _consumeControllerThread;

        private bool _mute = false;
        
        //buffers
        private short[] _frame;
        private byte[] _compressedFrame;
        private byte[] _packet;
        
        public KafkaVoiceChatNetworkModule(int id, string serverUrl, int roomNr, AudioFormat audioFormat, AudioCodec audioCodec) : base(id, serverUrl, audioFormat, audioCodec)
        {
            _serverKafkaUri = "kafka://" + serverUrl + ":9092";
            _serverRoomControlUri = "http://" + serverUrl + ":8080/ci/public/api/room/control/";
            _roomNr = roomNr;
            _serverTopicAudioData = "vcr-room-audio-" + _roomNr;
            _serverTopicController = "vcr-room-control-" + _roomNr;
            _frame = new short[AudioFormat.SamplesPerFrame];
            _compressedFrame = new byte[AudioFormat.SamplesPerFrame];

            var options = new ConsumerOptions(_serverTopicAudioData, new BrokerRouter(new KafkaOptions(new Uri(_serverKafkaUri))));
            options.MinimumBytes = 1;
            _audioDataConsumer = new Consumer(options);
            var offsets = _audioDataConsumer.GetTopicOffsetAsync(_serverTopicAudioData).Result
                .Select(x => new OffsetPosition(x.PartitionId, x.Offsets.Max())).ToArray();
            _audioDataConsumer = new Consumer(options, offsets);
            
            options = new ConsumerOptions(_serverTopicController, new BrokerRouter(new KafkaOptions(new Uri(_serverKafkaUri))));
            options.MinimumBytes = 1;
            _controllerConsumer = new Consumer(options);
            offsets = _controllerConsumer.GetTopicOffsetAsync(_serverTopicController).Result
                .Select(x => new OffsetPosition(x.PartitionId, x.Offsets.Max() - 1)).ToArray();
            _controllerConsumer = new Consumer(options, offsets);

            _consumeAudioDataThread = new Thread(ConsumeController);
            _consumeControllerThread = new Thread(ConsumeAudioData);
            _producer = new Producer(new BrokerRouter(new KafkaOptions(new Uri(_serverKafkaUri))));
        }

        public override void StartListenForFrames(AudioFrameBuffer audioFrameBuffer)
        {
            AudioFrameBuffer = audioFrameBuffer;
            _consumeControllerThread.Start();
            _consumeAudioDataThread.Start();
        }

        public override void StopListenForFrames()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Stopping consumer..");
            _consumeControllerThread.Abort();
            _consumeAudioDataThread.Abort();
            //join thread to wait for it to be aborted
            _consumeControllerThread.Join();
            _consumeAudioDataThread.Join();
        }

        public override void SendFrame(short[] frame, bool newCodecState = false)
        {
            if (_mute) return; // if muted then dont send anything
            var len = AudioCodec.Encode(frame, _compressedFrame, Id);
            _packet = new byte[len + 2];
            _packet[0] = Convert.ToByte(Id);
            _packet[1] = Convert.ToByte(newCodecState);
            if (newCodecState) AudioCodec.ResetEncoder(Id);
            Array.Copy(_compressedFrame, 0, _packet, 2, len);
            _producer.SendMessageAsync(_serverTopicAudioData, new[] {new Message(_packet)});
        }

        /// <summary>
        /// Consumes frames from the kafka server.
        /// This is meant to be ran on its own thread.
        /// </summary> 
        private void ConsumeAudioData()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Starting kafka consumer on server: " + _serverKafkaUri + " topic: " + _serverTopicAudioData + " " + _audioDataConsumer.GetOffsetPosition()[0]);
            int headerId;
            bool newCodecState;
            foreach (var message in _audioDataConsumer.Consume())
            {
                headerId = Convert.ToInt32(message.Value[0]);
                newCodecState = Convert.ToBoolean(message.Value[1]);
                if (newCodecState) AudioCodec.ResetDecoder(headerId);
                _frame = new short[AudioFormat.SamplesPerFrame]; //each entry in the buffer needs to be a new reference, this should probably be changed at some point
                AudioCodec.Decode(message.Value.Skip(2).ToArray(), _frame, headerId);
                AudioFrameBuffer.AddFrameToBuffer(_frame, headerId);
            }
        }

        /// <summary>
        /// Consume the controller topic
        /// </summary>
        private void ConsumeController()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Starting kafka consumer on server: " + _serverKafkaUri + " topic: " + _serverTopicController + " " + _audioDataConsumer.GetOffsetPosition()[0]);
            foreach (var message in _controllerConsumer.Consume())
            {
                var tmp = JToken.Parse(Encoding.UTF8.GetString(message.Value))["audio_off_users"].Values<int>().Contains(Id);
                if (tmp != _mute)
                {
                    if (tmp) VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "You have been muted.");
                    else VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "You have been un muted.");
                }
                _mute = tmp;
            }
        }

        public IEnumerator MuteUser(int userId, string userToken)
        {
            var form = new WWWForm();
            form.AddField("room_id", _roomNr);
            if (userId != Id) form.AddField("user_id", userId);

            var request = UnityWebRequest.Post(_serverRoomControlUri + "offAudio", form);
            request.SetRequestHeader("Authorization", userToken);

            yield return request.SendWebRequest();
        
            if (request.isNetworkError || request.isHttpError)
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.Warning, request.error);
            }
        }
        
        public IEnumerator UnMuteUser(int userId, string userToken)
        {
            var form = new WWWForm();
            form.AddField("room_id", _roomNr);
            if (userId != Id) form.AddField("user_id", userId);

            var request = UnityWebRequest.Post(_serverRoomControlUri + "onAudio", form);
            request.SetRequestHeader("Authorization", userToken);

            yield return request.SendWebRequest();
        
            if (request.isNetworkError || request.isHttpError)
            {
                VoiceChatUtils.Log(VoiceChatUtils.LogType.Warning, request.error);
            }
        }
        
    }
}