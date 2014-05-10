using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	class DecoderFactory
	{
		public delegate IDecoder DecoderCreator(byte[] data);
		private readonly Dictionary<string, DecoderCreator> Decoders;

		public DecoderFactory()
		{
			Decoders = new Dictionary<string, DecoderCreator>();
		}

		/// <summary>
		/// Register a new decoder
		/// </summary>
		/// <param name="extension">Extension of the deocder files, ie ".ogg"</param>
		/// <param name="creator">Creator function for the decoder</param>
		public void Register(string extension, DecoderCreator creator)
		{
			if (string.IsNullOrEmpty(extension))
			{
				throw new ArgumentException("extension is null or empty");
			}

			if (creator == null)
			{
				throw new ArgumentNullException("creator");
			}

			Decoders.Add(extension, creator);
		}

		/// <summary>
		/// Creates a new decoder
		/// </summary>
		/// <param name="extension">Extension of the decoder to use</param>
		/// <param name="data">Array with the data to decode</param>
		/// <returns>A decoder for the byte data</returns>
		public IDecoder Create(string extension, byte[] data)
		{
			return Decoders[extension](data);
		}
	}
}
