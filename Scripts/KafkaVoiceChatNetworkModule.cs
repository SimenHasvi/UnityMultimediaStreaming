using System;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using UnityEngine;

namespace VoiceChat
{
    /// <summary>
    /// Networking module using kafka as the backend
    /// </summary>
    public class KafkaVoiceChatNetworkModule : VoiceChatNetworkModule
    {
        private readonly string _serverTopic;
        private readonly Consumer _consumer;
        private readonly Producer _producer;
        private readonly Thread _consumeThread;
        
        public KafkaVoiceChatNetworkModule(int id, string serverUri, string serverTopic, AudioCodec audioCodec) : base(id, serverUri, audioCodec)
        {
            _serverTopic = serverTopic;
            var options = new ConsumerOptions(serverTopic, new BrokerRouter(new KafkaOptions(new Uri(ServerUri))));
            options.MinimumBytes = 1;
            _consumer = new Consumer(options);
            var offsets = _consumer.GetTopicOffsetAsync(serverTopic).Result
                .Select(x => new OffsetPosition(x.PartitionId, x.Offsets.Max())).ToArray();
            _consumer = new Consumer(options, offsets);
            _consumeThread = new Thread(Consume);
            _producer = new Producer(new BrokerRouter(new KafkaOptions(new Uri(ServerUri))));
        }

        public override void StartListenForFrames(AudioFormat audioFormat, AudioFrameBuffer audioFrameBuffer)
        {
            AudioFormat = audioFormat;
            AudioFrameBuffer = audioFrameBuffer;
            _consumeThread.Start();
        }

        public override void StopListenForFrames()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Stopping consumer..");
            _consumeThread.Abort();
            //join thread to wait for it to be aborted
            _consumeThread.Join();
        }

        public override void SendFrame(short[] frame)
        {
            var compressedFrame = AudioCodec.Encode(frame);
            var packet = new byte[compressedFrame.Length + 1];
            packet[0] = Convert.ToByte(Id);
            Array.Copy(compressedFrame, 0, packet, 1, compressedFrame.Length);
            _producer.SendMessageAsync(_serverTopic, new[] {new Message(packet)});
        }

        /// <summary>
        /// Consumes frames from the kafka server.
        /// This is meant to be ran on its own thread.
        /// </summary> 
        private void Consume()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Starting kafka consumer on server: " + ServerUri + " topic: " + _serverTopic + " " + _consumer.GetOffsetPosition()[0]);
            foreach (var message in _consumer.Consume())
            {
                var packet = message.Value;
                AudioFrameBuffer.AddFrameToBuffer(AudioCodec.Decode(packet.Skip(1).ToArray()), Convert.ToInt32(packet[0]));
            }
        }
    }
}