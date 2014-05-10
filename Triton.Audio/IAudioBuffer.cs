using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	interface IAudioBuffer : IDisposable
	{
		/// <summary>
		/// Bind a audio source to the buffer
		/// </summary>
		/// <param name="source">OpenAL id of the audio source</param>
		void Bind(int source);

		/// <summary>
		/// Update the buffer for a specific audio source, neccecary for streaming
		/// </summary>
		/// <param name="source">OpenAL id of the audio source</param>
		void Update(int source);
	}
}
