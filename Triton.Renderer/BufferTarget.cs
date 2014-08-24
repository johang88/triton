using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public enum BufferTarget
	{
		ArrayBuffer = 34962,
		ElementArrayBuffer = 34963,
		PixelPackBuffer = 35051,
		PixelUnpackBuffer = 35052,
		UniformBuffer = 35345,
		TextureBuffer = 35882,
		TransformFeedbackBuffer = 35982,
		CopyReadBuffer = 36662,
		CopyWriteBuffer = 36663,
		DrawIndirectBuffer = 36671,
		ShaderStorageBuffer = 37074,
		DispatchIndirectBuffer = 37102,
		QueryBuffer = 37266,
		AtomicCounterBuffer = 37568,
	}
}
