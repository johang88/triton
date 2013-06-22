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
		BoneIndex1,
		BoneIndex2,
		BoneIndex3,
		BoneIndex4,
		BoneWeight1,
		BoneWeight2,
		BoneWeight3,
		BoneWeight4,
	}
}
