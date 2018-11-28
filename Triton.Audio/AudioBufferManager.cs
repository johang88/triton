using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	class AudioBufferManager : IDisposable
	{
		private readonly IO.FileSystem FileSystem;
		private readonly DecoderFactory DecoderFactory;
		private readonly Dictionary<string, IAudioBuffer> Buffers;

		public AudioBufferManager(DecoderFactory decoderFactory, IO.FileSystem fileSystem)
		{
			if (decoderFactory == null)
				throw new ArgumentNullException("decoderFactory");

			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			DecoderFactory = decoderFactory;
			FileSystem = fileSystem;

			Buffers = new Dictionary<string, IAudioBuffer>();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var buffer in Buffers.Values)
				{
					buffer.Dispose();
				}

				Buffers.Clear();
			}
		}

		public IAudioBuffer GetBuffer(string filename)
		{
			if (Buffers.ContainsKey(filename))
			{
				return Buffers[filename];
			}

			using (var stream = FileSystem.OpenRead(filename))
			{
				byte[] data = Util.ReadAllDataFromStream(stream);

				var decoder = DecoderFactory.Create(Path.GetExtension(filename), data);

				IAudioBuffer buffer = null;

				if (decoder.IsStreamingPrefered)
				{
					buffer = new Buffers.StreamedAudioBuffer(decoder);
				}
				else
				{
					buffer = new Buffers.AudioBuffer(decoder);
				}

				return buffer;
			}
		}

		public void RemoveBuffer(string name)
		{
			var buffer = Buffers[name];
			Buffers.Remove(name);

			buffer.Dispose();
		}

		public IEnumerable<KeyValuePair<string, IAudioBuffer>> GetBuffers()
		{
			return Buffers;
		}
	}
}
