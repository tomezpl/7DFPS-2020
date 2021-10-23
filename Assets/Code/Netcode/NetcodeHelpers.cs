using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class NetcodeHelpers
{
    public static class StreamHelper
    {
        /// <summary>
        /// Writes the provided <see cref="Vector3"/> to the stream. Make sure to seek the stream to the right offset.
        /// </summary>
        /// <param name="outputStream">The <see cref="Stream"/> to write <paramref name="position"/> data to.</param>
        /// <param name="position">The <see cref="Vector3"/> to write to the <paramref name="outputStream"/>.</param>
        public static void WritePosition(Stream outputStream, Vector3? position = null)
        {
            // Failsafe
            if(position == null)
            {
                position = Vector3.zero;
            }

            byte[] posBytes = new byte[sizeof(float) * 3];

            for(int i = 0; i < 3; i++)
            {
                Array.Copy(BitConverter.GetBytes(position.Value[i]), 0, posBytes, i * sizeof(float), sizeof(float));
            }

            outputStream.Write(posBytes, 0, posBytes.Length);
        }

        /// <summary>
        /// Writes the provided <see cref="Quaternion"/> to the stream. Make sure to seek the stream to the right offset.
        /// </summary>
        /// <param name="outputStream">The <see cref="Stream"/> to write <paramref name="orientation"/> data to.</param>
        /// <param name="orientation">The <see cref="Quaternion"/> to write into <paramref name="outputStream"/>.</param>
        public static void WriteOrientation(Stream outputStream, Quaternion? orientation = null)
        {
            // Failsafe
            if (orientation == null)
            {
                orientation = Quaternion.identity;
            }

            byte[] quatBytes = new byte[sizeof(float) * 4];

            for (int i = 0; i < 4; i++)
            {
                Array.Copy(BitConverter.GetBytes(orientation.Value[i]), 0, quatBytes, i * sizeof(float), sizeof(float));
            }

            outputStream.Write(quatBytes, 0, quatBytes.Length);
        }

        /// <summary>
        /// Reads a <see cref="Vector3"/> from the current stream offest. Make sure to seek the stream to the right offset.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream"/> containing the <see cref="Vector3"/>.</param>
        /// <returns>A <see cref="Vector3"/> constructed from the bytes at this stream offset.</returns>
        public static Vector3 ReadPosition(Stream inputStream)
        {
            Vector3 output = new Vector3();
            if(inputStream.CanRead)
            {
                byte[] posBytes = new byte[sizeof(float) * 3];

                inputStream.Read(posBytes, 0, sizeof(float) * 3);
                
                for(int i = 0; i < 3; i++)
                {
                    output[i] = BitConverter.ToSingle(posBytes, i * sizeof(float));
                }
            }

            return output;
        }

        /// <summary>
        /// Reads a <see cref="Quaternion"/> from the current stream offest. Make sure to seek the stream to the right offset.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream"/> containing the <see cref="Quaternion"/>.</param>
        /// <returns>A <see cref="Quaternion"/> constructed from the bytes at this stream offset.</returns>
        public static Quaternion ReadOrientation(Stream inputStream)
        {
            Quaternion output = Quaternion.identity;
            if (inputStream.CanRead)
            {
                byte[] quatBytes = new byte[sizeof(float) * 4];

                inputStream.Read(quatBytes, 0, sizeof(float) * 4);

                for (int i = 0; i < 4; i++)
                {
                    output[i] = BitConverter.ToSingle(quatBytes, i * sizeof(float));
                }
            }

            return output;
        }

        public static void WriteGameModeExtensions(MemoryStream ms, string name)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(name ?? "");
            writer.Flush();
        }

        /// <summary>
        /// Reads a <see cref="string"/> from the current stream offest. Make sure to seek the stream to the right offset prior to calling.
        /// </summary>
        /// <param name="inputStream">The <see cref="Stream"/> containing the <see cref="string"/>.</param>
        /// <returns>A <see cref="string"/> constructed from the bytes at this stream offset.</returns>
        public static string ReadGameModeExtensions(Stream inputStream)
        {
            string output = "";
            if (inputStream.CanRead)
            {
                using (StreamReader reader = new StreamReader(inputStream))
                {
                    output = reader.ReadToEnd();
                }
            }

            return output;
        }
    }
}