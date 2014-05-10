using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio.Decoders
{
	class OggDecoder : IDecoder
	{
		public Format Format { get; private set; }
		public int Frequency { get; private set; }
		public bool IsStreamingPrefered { get; private set; }
		private NVorbis.VorbisReader Reader;

		public OggDecoder(byte[] data)
		{
			var stream = new System.IO.MemoryStream(data);
			Reader = new NVorbis.VorbisReader(stream, true);

			if (Reader.Channels == 1)
				Format = Audio.Format.Mono16;
			else
				Format = Audio.Format.Stereo16;

			Frequency = Reader.SampleRate;
			IsStreamingPrefered = Reader.TotalTime.TotalSeconds > 10.0f;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				Reader.Dispose();
			}
		}

		public int ReadSamples(float[] buffer, int offset, int count)
		{
			return Reader.ReadSamples(buffer, offset, count);
		}

		public long TotalSamples { get { return Reader.TotalSamples; } }
	}
}
