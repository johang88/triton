using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public enum VertexFormatSemantic : byte
	{
		Position = 0,
		Normal,
		Tangent,
		TexCoord,
		TexCoord2,
		Color,
		BoneIndex,
		BoneWeight,
		Last
	}
}
