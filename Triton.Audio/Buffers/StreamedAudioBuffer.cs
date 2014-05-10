using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio.Buffers
{
	class StreamedAudioBuffer : IAudioBuffer
	{
		const int BUFFER_SIZE = 44100;
		const int NUMBER_OF_BUFFERS = 10;

		private readonly int[] Buffers = new int[NUMBER_OF_BUFFERS];
		private readonly IDecoder Decoder;
		private readonly float[] ReadBuffer = new float[BUFFER_SIZE];
		private readonly short[] CastBuffer = new short[BUFFER_SIZE];

		public StreamedAudioBuffer(IDecoder decoder)
		{
			if (decoder == null)
				throw new ArgumentNullException("decoder");

			Decoder = decoder;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				Decoder.Dispose();

				AL.DeleteBuffers(Buffers);
				Util.CheckOpenAlErrors();
			}
		}

		public void Bind(int source)
		{
			AL.GenBuffers(Buffers.Length, out Buffers[0]);
			Util.CheckOpenAlErrors();

			for (int i = 0; i < Buffers.Length; i++)
			{
				Stream(Buffers[i]);
			}

			AL.SourceQueueBuffers(source, Buffers.Length, Buffers);
			Util.CheckOpenAlErrors();
		}

		void Stream(int buffer)
		{
			int size = 0;
			while (size < BUFFER_SIZE)
			{
				int result = Decoder.ReadSamples(ReadBuffer, size, BUFFER_SIZE - size);

				if (result > 0)
				{
					size += result;
				}
				else
				{
					break;
				}
			}

			Util.CastBuffer(ReadBuffer, CastBuffer, size);

			AL.BufferData(buffer, Util.ToOpenAL(Decoder.Format), CastBuffer, size * sizeof(short), Decoder.Frequency);
			Util.CheckOpenAlErrors();
		}

		public void Update(int source)
		{
			int processed;
			AL.GetSource(source, ALGetSourcei.BuffersProcessed, out processed);
			Util.CheckOpenAlErrors();

			while ((processed--) > 0)
			{
				var buffer = AL.SourceUnqueueBuffer(source);
				Util.CheckOpenAlErrors();

				Stream(buffer);

				AL.SourceQueueBuffer(source, buffer);
				Util.CheckOpenAlErrors();
			}
		}
	}
}
