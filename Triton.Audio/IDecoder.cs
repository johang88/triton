using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	public interface IDecoder : IDisposable
	{
		Format Format { get; }
		int Frequency { get; }
		bool IsStreamingPrefered { get; }
		int ReadSamples(float[] buffer, int offset, int count);
		long TotalSamples { get; }
	}
}
