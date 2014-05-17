using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer.RenderTargets
{
	public class Definition
	{
		public readonly int Width;
		public readonly int Height;
		public readonly List<Attachment> Attachments = new List<Attachment>();
		public readonly bool RenderDepthToTexture;
		public readonly bool RenderToCubeMap = false;

		public Definition(int width, int height, bool renderDepthToTexture, List<Attachment> attachments, bool renderToCubeMap = false)
		{
			Width = width;
			Height = height;
			Attachments = attachments;
			RenderDepthToTexture = renderDepthToTexture;
			RenderToCubeMap = renderToCubeMap;
		}

		public class Attachment
		{
			public AttachmentPoint AttachmentPoint;
			public PixelFormat PixelFormat;
			public PixelInternalFormat PixelInternalFormat;
			public PixelType PixelType;
			public int Index = 0;
			internal int TextureHandle = 0;

			public Attachment(AttachmentPoint attachmentPoint, PixelFormat pixelFormat, PixelInternalFormat pixelInternalFormat, PixelType pixelType, int index = 0)
			{
				AttachmentPoint = attachmentPoint;
				PixelFormat = pixelFormat;
				PixelInternalFormat = pixelInternalFormat;
				PixelType = pixelType;
				Index = index;
			}
		}

		public enum AttachmentPoint
		{
			/// <summary>
			/// Index = color attachment index
			/// </summary>
			Color,
			/// <summary>
			/// Index = if set to > 0 then it will be interpreted as a render target handle and will use the same depth buffer as that target
			/// </summary>
			Depth
		}
	}
}
