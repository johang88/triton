using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public class VertexFormatElement
	{
		public readonly VertexFormatSemantic Semantic;
		public readonly VertexPointerType Type;
		public readonly byte Count;
		public readonly short Offset;

		public VertexFormatElement(VertexFormatSemantic semantic, VertexPointerType type, byte count, short offset)
		{
			Semantic = semantic;
			Type = type;
			Count = count;
			Offset = offset;
		}
	}
}
