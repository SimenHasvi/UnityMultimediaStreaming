using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat
{
    /// <summary>
    /// A few utilities which can be useful for a voice chat project
    /// </summary>
    public static class VoiceChatUtils
    {
        /// <summary>
        /// Types of log messages
        /// </summary>
        public enum LogType {Info, VerboseInfo, Warning}
        
        /// <summary>
        /// Delegate for the event of a log message received
        /// </summary>
        public delegate void LogMsgReceivedDelegate(LogType type, string msg);
        
        /// <summary>
        /// Called in the even of a log message received
        /// </summary>
        public static event LogMsgReceivedDelegate onLogMsgReceived;
        
        /// <summary>
        /// Call to log certain info. What do to with the messages is determined by <see cref="onLogMsgReceived"/>
        /// </summary>
        /// <param name="type">The type of log message.</param>
        /// <param name="msg">The message.</param>
        public static void Log(LogType type, string msg)
        {
            onLogMsgReceived?.Invoke(type, msg);
        }

        /// <summary>
        /// Write the log messages in the unity console.
        /// </summary>
        /// <param name="verbose">Whether to write the verbose messages or not.</param>
        public static void EnableUnityLogging(bool verbose)
        {
            onLogMsgReceived += (type, msg) =>
            {
                switch (type)
                {
                    case LogType.Info:Debug.Log(msg); break;
                    case LogType.VerboseInfo: if (verbose) Debug.Log(msg); break;
                    case LogType.Warning: Debug.LogWarning(msg); break;
                }
            };
        }
        
        /// <summary>
        /// Convert a float array with range -1 to 1 to a short array.
        /// </summary>
        /// <param name="floats">The float array (values between -1 and 1)</param>
        /// <returns>The short array.</returns>
        public static short[] FloatToShort(params float[] floats)
        {
            var shorts = new short[floats.Length];
            for (var i = 0; i < floats.Length; i++)
            {
                shorts[i] = (short)(floats[i] * short.MaxValue);
            }
            return shorts;
        }

        /// <summary>
        /// Convert a float array with range -1 to 1 to a short array.
        /// Make sure the two array have the same length.
        /// </summary>
        /// <param name="outShorts">The array where we write the output</param>
        /// <param name="floats">The float array (values between -1 and 1)</param>
        /// <returns>The short array.</returns>
        public static void FloatToShort(short[] outShorts, params float[] floats)
        {
            for (var i = 0; i < floats.Length; i++)
            {
                outShorts[i] = (short)(floats[i] * short.MaxValue);
            }
        }

        /// <summary>
        /// Convert a short array to a float array (with range -1 to 1).
        /// </summary>
        /// <param name="shorts">The short array.</param>
        /// <returns>The resulting float array.</returns>
        public static float[] ShortToFloat(params short[] shorts)
        {
            var floats = new float[shorts.Length];
            for (var i = 0; i < shorts.Length; i++)
            {
                floats[i] = shorts[i] / (float)short.MaxValue;
            }
            return floats;
        }

        /// <summary>
        /// Convert a short array to a float array (with range -1 to 1).
        /// Make sure the two arrays have the same length.
        /// </summary>
        /// <param name="outFloats">The array to write the results to.</param>
        /// <param name="shorts">The short array.</param>
        public static void ShortToFloat(float[] outFloats, params short[] shorts)
        {
            for (var i = 0; i < shorts.Length; i++)
            {
                outFloats[i] = shorts[i] / (float)short.MaxValue;
            }
        }

        /// <summary>
        /// Copy the raw bytes of a short array to a byte array
        /// </summary>
        /// <param name="shorts">The short array.</param>
        /// <returns>The resulting byte array.</returns>
        public static byte[] ToByteStream(params short[] shorts)
        {
            var bytes = new byte[shorts.Length * sizeof(short)];
            Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Copy the raw bytes of a short array to a byte array
        /// </summary>
        /// <param name="outBytes">Bytes of the short array, should be length shorts.Length * sizeof(short)</param>
        /// <param name="shorts">The short array.</param>
        public static void ToByteStream(byte[] outBytes, params short[] shorts)
        {
            Buffer.BlockCopy(shorts, 0, outBytes, 0, outBytes.Length);
        }

        /// <summary>
        /// Copy the raw bytes of a byte array to a short array.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The resulting short array.</returns>
        public static short[] FromByteStream(params byte[] bytes)
        {
            var shorts = new short[bytes.Length / sizeof(short)];
            Buffer.BlockCopy(bytes, 0, shorts, 0, bytes.Length);
            return shorts;
        }

        /// <summary>
        /// Copy the raw bytes of a byte array to a short array.
        /// </summary>
        /// <param name="outShorts">output buffer, should have length bytes.Length / sizeof(short)</param>
        /// <param name="bytes">The bytes.</param>
        public static void FromByteStream(short[] outShorts, params byte[] bytes)
        {
            Buffer.BlockCopy(bytes, 0, outShorts, 0, bytes.Length);
        }
        
        /// <summary>
        /// Get the distance from a point to another in a circular manner. Meaning that it will wrap around at a given point.
        /// We assume that <see cref="to"/> is always ahead of <see cref="from"/>.
        /// This function is mostly useful for circular buffers.
        /// </summary>
        /// <param name="from">The from-point.</param>
        /// <param name="to">The to-point.</param>
        /// <param name="circumference"> The point in which the wrap back around to 0.</param>
        /// <returns>The distance between the two points, in a single direction, with wrap around at <see cref="circumference"/></returns>
        public static int CircularDistanceTo(int from, int to, int circumference)
        {
            if (to >= from)
            {
                return to - from;
            }
            return circumference - (from - to);
        }
    }
}