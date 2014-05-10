using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio.Buffers
{
	class AudioBuffer : IAudioBuffer
	{
		private readonly int Buffer;
		const int MIN_BUFFER_SIZE = 4096;

		public AudioBuffer(IDecoder decoder)
		{
			if (decoder == null)
				throw new ArgumentNullException("decoder");

			Buffer = AL.GenBuffer();
			Util.CheckOpenAlErrors();

			var data = new float[decoder.TotalSamples];
			var castData = new short[decoder.TotalSamples];
			
			int read = 0;
			while (read < data.Length)
			{
				read += decoder.ReadSamples(data, read, data.Length - read);
			}

			Util.CastBuffer(data, castData, data.Length);

			AL.BufferData(Buffer, Util.ToOpenAL(decoder.Format), castData, castData.Length * sizeof(short), decoder.Frequency);
			Util.CheckOpenAlErrors();

			decoder.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				AL.DeleteBuffer(Buffer);
				Util.CheckOpenAlErrors();
			}
		}

		public void Bind(int source)
		{
			AL.Source(source, ALSourcei.Buffer, Buffer);
			Util.CheckOpenAlErrors();
		}

		public void Update(int source)
		{
			// Nothing to do here
		}
	}
}
