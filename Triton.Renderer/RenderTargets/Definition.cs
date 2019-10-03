using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer.RenderTargets
{
    public class Definition
    {
        public int Width { get; }
        public int Height { get; }
        public List<Attachment> Attachments { get; } = new List<Attachment>();
        public bool RenderDepthToTexture { get; }
        public bool RenderToCubeMap { get; }

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
            public AttachmentPoint AttachmentPoint { get; set; }
            public PixelFormat PixelFormat { get; set; }
            public PixelInternalFormat PixelInternalFormat { get; set; }
            public PixelType PixelType { get; set; }
            public int Index { get; set; } = 0;
            internal int TextureHandle { get; set; } = 0;
            public bool MipMaps { get; set; } = false;

            public Attachment(AttachmentPoint attachmentPoint, PixelFormat pixelFormat, PixelInternalFormat pixelInternalFormat, PixelType pixelType, int index = 0, bool mipmaps = false)
            {
                AttachmentPoint = attachmentPoint;
                PixelFormat = pixelFormat;
                PixelInternalFormat = pixelInternalFormat;
                PixelType = pixelType;
                Index = index;
                MipMaps = mipmaps;
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
