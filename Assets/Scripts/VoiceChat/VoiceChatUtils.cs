using System;

namespace VoiceChat
{
    /// <summary>
    /// A few utilities which can be useful for a voice chat project
    /// </summary>
    public static class VoiceChatUtils
    {
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