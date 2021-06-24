using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        private readonly Stopwatch _stopwatch = new Stopwatch();
        
        //buffers
        private short[] _frame;
        private byte[] _compressedFrame;
        private byte[] _packet;
        
        public KafkaVoiceChatNetworkModule(int id, string serverUri, string serverTopic, AudioFormat audioFormat, AudioCodec audioCodec) : base(id, serverUri, audioFormat, audioCodec)
        {
            _serverTopic = serverTopic;
            _frame = new short[AudioFormat.SamplesPerFrame];
            _compressedFrame = new byte[AudioFormat.SamplesPerFrame];
            _packet = new byte[AudioFormat.SamplesPerFrame + 1];
            var options = new ConsumerOptions(serverTopic, new BrokerRouter(new KafkaOptions(new Uri(ServerUri))));
            options.MinimumBytes = 1;
            _consumer = new Consumer(options);
            var offsets = _consumer.GetTopicOffsetAsync(serverTopic).Result
                .Select(x => new OffsetPosition(x.PartitionId, x.Offsets.Max())).ToArray();
            _consumer = new Consumer(options, offsets);
            _consumeThread = new Thread(Consume);
            _producer = new Producer(new BrokerRouter(new KafkaOptions(new Uri(ServerUri))));
        }

        public override void StartListenForFrames(AudioFrameBuffer audioFrameBuffer)
        {
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
            //if (_stopwatch.ElapsedMilliseconds >= AudioFormat.MillisecondsPerFrame * 2) AudioCodec.ResetEncoder(Id);
            //_stopwatch.Restart();
            var len = AudioCodec.Encode(frame, _compressedFrame, Id);
            _packet = new byte[len + 1];
            _packet[0] = Convert.ToByte(Id);
            Array.Copy(_compressedFrame, 0, _packet, 1, len);
            _producer.SendMessageAsync(_serverTopic, new[] {new Message(_packet)});
        }

        /// <summary>
        /// Consumes frames from the kafka server.
        /// This is meant to be ran on its own thread.
        /// </summary> 
        private void Consume()
        {
            VoiceChatUtils.Log(VoiceChatUtils.LogType.Info, "Starting kafka consumer on server: " + ServerUri + " topic: " + _serverTopic + " " + _consumer.GetOffsetPosition()[0]);
            int headerId;
            foreach (var message in _consumer.Consume())
            {
                headerId = Convert.ToInt32(message.Value[0]);
                _frame = new short[AudioFormat.SamplesPerFrame]; //each entry in the buffer needs to be a new reference, this should probably be changed at some point.
                //if (AudioFrameBuffer.Count(headerId) <= 0) AudioCodec.ResetDecoder(headerId); //we assume a break in transmission so we reset the codec
                AudioCodec.Decode(message.Value.Skip(1).ToArray(), _frame, headerId);
                AudioFrameBuffer.AddFrameToBuffer(_frame, headerId);
            }
        }
    }
}