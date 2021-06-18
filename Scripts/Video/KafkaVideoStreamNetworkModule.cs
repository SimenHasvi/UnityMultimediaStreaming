using System;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using VoiceChat;

namespace UnityVoiceChat.Scripts.Video
{
    public class KafkaVideoStreamNetworkModule : VideoStreamNetworkModule
    {
        private readonly string _serverTopic;
        private readonly Consumer _consumer;
        private readonly Producer _producer;
        private readonly Thread _consumeThread;
        
        public KafkaVideoStreamNetworkModule(int id, string serverUri, string serverTopic) : base(id, serverUri)
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

        public override void StartListenForFrames(VideoFrameBuffer videoFrameBuffer)
        {
            VideoFrameBuffer = videoFrameBuffer;
            _consumeThread.Start();
        }

        public override void StopListenForFrames()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Stopping consumer..");
            _consumeThread.Abort();
            //join thread to wait for it to be aborted
            _consumeThread.Join();
        }

        public override void SendFrame(byte[] frame)
        {
            var packet = new byte[frame.Length + 1];
            packet[0] = Convert.ToByte(Id);
            Array.Copy(frame, 0, packet, 1, frame.Length);
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
                VideoFrameBuffer.AddFrameToBuffer(packet.Skip(1).ToArray(), Convert.ToInt32(packet[0]));
            }
        }
        
    }
}