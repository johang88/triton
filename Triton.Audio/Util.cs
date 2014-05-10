using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	static class Util
	{
		public static ALFormat ToOpenAL(Format format)
		{
			switch (format)
			{
				case Format.Mono16:
					return ALFormat.Mono16;
				case Format.Stereo16:
					return ALFormat.Stereo16;
				default:
					throw new Exception("unknown format");
			}
		}

		public static void CheckOpenAlErrors()
		{
			ALError error = AL.GetError();
			if (error > 0)
			{
				throw new Exception("OpenAL error: " + error.ToString());
			}
		}

		public static byte[] ReadAllDataFromStream(Stream stream)
		{
			int size = (int)stream.Length;
			byte[] data = new byte[size];

			int bytes = 0;
			int offset = 0;

			do
			{
				bytes = stream.Read(data, offset, size - offset);
				offset += bytes;
			} while (bytes > 0);

			return data;
		}

		public static void CastBuffer(float[] inBuffer, short[] outBuffer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				var temp = (int)(32767f * inBuffer[i]);
				if (temp > short.MaxValue) 
					temp = short.MaxValue;
				else if (temp < short.MinValue) 
					temp = short.MinValue;
				outBuffer[i] = (short)temp;
			}
		}
	}
}
