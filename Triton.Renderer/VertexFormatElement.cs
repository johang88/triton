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
        public readonly short Divisor;
        public readonly bool Normalized;

        public VertexFormatElement(VertexFormatSemantic semantic, VertexPointerType type, byte count, short offset, short divisor = 0, bool normalized = false)
        {
            Semantic = semantic;
            Type = type;
            Count = count;
            Offset = offset;
            Divisor = divisor;
            Normalized = normalized;
        }
    }
}
