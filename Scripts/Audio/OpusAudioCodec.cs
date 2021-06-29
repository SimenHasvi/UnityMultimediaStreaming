using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Concentus.Enums;
using Concentus.Structs;
using UnityEngine;

namespace VoiceChat
{
    public class OpusAudioCodec : AudioCodec
    {

        /// <summary>
        /// Encoders and decoders for each user.
        /// We need to do this as the Opus codec is stateful and will use the previous frame for its compression.
        /// So we cant use the same object for all the users.
        /// In general only one encoder is needed but I still add support for multiple anyways.
        /// </summary>
        private readonly Dictionary<int, OpusEncoder> _encoders = new Dictionary<int, OpusEncoder>();
        private readonly Dictionary<int, OpusDecoder> _decoders = new Dictionary<int, OpusDecoder>();

        public OpusAudioCodec(AudioFormat audioFormat, int bitrate, int complexity) : base(audioFormat, bitrate, complexity) { }

        /// <summary>
        /// Add a new encoder
        /// </summary>
        /// <param name="id">The id of the user to use this encoder.</param>
        private void AddEncoder(int id)
        {
            if (_encoders.ContainsKey(id)) return;
            _encoders.Add(id, new OpusEncoder(AudioFormat.SamplingRate, AudioFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP)
            {
                Bitrate = Bitrate,
                SignalType = OpusSignal.OPUS_SIGNAL_VOICE,
                ForceMode = OpusMode.MODE_SILK_ONLY,
                UseVBR = true,
                //UseDTX = true,
                Complexity = Complexity
            });
        }

        /// <summary>
        /// Add a new decoder
        /// </summary>
        /// <param name="id">The id of the user who use this decoder</param>
        private void AddDecoder(int id)
        {
            if (_decoders.ContainsKey(id)) return;
            _decoders.Add(id, new OpusDecoder(AudioFormat.SamplingRate, AudioFormat.Channels));
        }

        public override byte[] Encode(short[] frame, int id = 0)
        {
            if (!_encoders.ContainsKey(id)) AddEncoder(id);
            var compressedFrame = new byte[frame.Length];
            var len = _encoders[id].Encode(frame, 0, frame.Length, compressedFrame, 0, frame.Length);
            Array.Resize(ref compressedFrame, len);
            return compressedFrame;
        }

        public override int Encode(short[] frame, byte[] compressedFrame, int id = 0)
        {
            if (!_encoders.ContainsKey(id)) AddEncoder(id);
            var len = _encoders[id].Encode(frame, 0, frame.Length, compressedFrame, 0, frame.Length);
            Array.Resize(ref compressedFrame, len);
            return len;
        }

        public override short[] Decode(byte[] compressedFrame, int id = 0)
        {
            if (!_decoders.ContainsKey(id)) AddDecoder(id);
            var frame = new short[AudioFormat.SamplesPerFrame];
            _decoders[id].Decode(compressedFrame, 0, compressedFrame.Length, frame, 0, frame.Length);
            return frame;
        }

        public override void Decode(byte[] compressedFrame, short[] frame, int id = 0)
        {
            if (!_decoders.ContainsKey(id)) AddDecoder(id);
            _decoders[id].Decode(compressedFrame, 0, compressedFrame.Length, frame, 0, frame.Length);
        }

        public override void ResetEncoder(int id)
        {
            _encoders[id].ResetState();
        }

        public override void ResetDecoder(int id)
        {
            _decoders[id].ResetState();
        }

        public override void Reset()
        {
            foreach (var encoder in _encoders)
            {
                encoder.Value.ResetState();
            }
            foreach (var decoder in _decoders)
            {
                decoder.Value.ResetState();
            }
        }
    }
}