using System;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;

namespace VoiceChat
{
    /// <summary>
    /// Networking module using kafka as the backend
    /// </summary>
    public class KafkaVoiceChatNetworkModule : VoiceChatNetworkModule
    {
        private string ServerTopic;
        private Consumer _consumer;
        
        private Producer _producer;
        private Thread _consumeThread;
        
        public KafkaVoiceChatNetworkModule(int id, string serverUri, string serverTopic, AudioCodec audioCodec) : base(id, serverUri, audioCodec)
        {
            ServerTopic = serverTopic;
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
            _producer.SendMessageAsync(ServerTopic, new[] {new Message(packet)});
        }

        /// <summary>
        /// Consumes frames from the kafka server.
        /// This is meant to be ran on its own thread.
        /// </summary>
        private void Consume()
        {
            foreach (var message in _consumer.Consume())
            {
                var packet = message.Value;
                AudioCodec.Decode(packet.Skip(1).ToArray());
                AudioFrameBuffer.AddFrameToBuffer(AudioCodec.Decode(packet.Skip(1).ToArray()), Convert.ToInt32(packet[0]));
            }
        }
    }
}