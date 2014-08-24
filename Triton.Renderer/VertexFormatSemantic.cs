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
		// InstanceTransform0-3 stores a per instance 4x4 matrix
		InstanceTransform0,
		InstanceTransform1,
		InstanceTransform2,
		InstanceTransform3,
		// Can be used to store extra per instance data like texture id etc ... 
		InstanceMisc,
		Last
	}
}
