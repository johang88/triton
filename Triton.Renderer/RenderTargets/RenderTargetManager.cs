using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.RenderTargets
{
	class RenderTargetManager : IDisposable
	{
		const int MaxHandles = 64;
		private readonly RenderTargetData[] Handles = new RenderTargetData[MaxHandles];
		private short NextFree = 0;
		private bool Disposed = false;
		private readonly object Lock = new object();

		public RenderTargetManager()
		{
			// Each empty handle will store the location of the next empty handle 
			for (var i = 0; i < Handles.Length; i++)
			{
				Handles[i].Id = (short)(i + 1);
			}

			Handles[Handles.Length - 1].Id = -1;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (!isDisposing || Disposed)
				return;

			for (var i = 0; i < Handles.Length; i++)
			{
				if (Handles[i].Initialized)
				{
					if (Handles[i].DepthBufferObject != 0 && !Handles[i].SharedDepth)
						GL.DeleteRenderbuffers(1, ref Handles[i].DepthBufferObject);
					GL.DeleteFramebuffers(1, ref Handles[i].FrameBufferObject);
				}
				Handles[i].Initialized = false;
			}

			Disposed = true;
		}

		int CreateHandle(int index, int id)
		{
			return (index << 16) | id;
		}

		void ExtractHandle(int handle, out int index, out int id)
		{
			id = handle & 0x0000FFFF;
			index = handle >> 16;
		}

		public int Create()
		{
			if (NextFree == -1)
			{
				return CreateHandle(-1, -1);
			}

			int index;
			lock (Lock)
			{
				index = NextFree;
				NextFree = Handles[NextFree].Id;
			}

			var id = ++Handles[index].Id;
			Handles[index].Initialized = false;

			return CreateHandle(index, id);
		}

		public void Init(int handle, int width, int height, int[] textureHandles, bool createDepthBuffer, int? sharedDepthHandle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			GL.GenFramebuffers(1, out Handles[index].FrameBufferObject);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handles[index].FrameBufferObject);

			Handles[index].DrawBuffers = new DrawBuffersEnum[textureHandles.Length];

			for (var i = 0; i < textureHandles.Length; i++)
			{
				Handles[index].DrawBuffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, OGL.TextureTarget.Texture2D, textureHandles[i], 0);
			}

			Handles[index].DepthBufferObject = 0;
			Handles[index].SharedDepth = false;

			if (createDepthBuffer)
			{
				GL.GenRenderbuffers(1, out Handles[index].DepthBufferObject);
				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handles[index].DepthBufferObject);
				GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, Handles[index].DepthBufferObject);
			}
			else if (sharedDepthHandle.HasValue)
			{
				var depthHandle = sharedDepthHandle.Value;
				int depthIndex, depthId;
				ExtractHandle(depthHandle, out depthIndex, out depthId);

				Handles[index].DepthBufferObject = Handles[depthIndex].DepthBufferObject;
				Handles[index].SharedDepth = true;

				GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handles[index].DepthBufferObject);
				GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, Handles[index].DepthBufferObject);
			}

			var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != FramebufferErrorCode.FramebufferComplete)
			{
				if (Handles[index].DepthBufferObject != 0)
					GL.DeleteRenderbuffers(1, ref Handles[index].DepthBufferObject);
				GL.DeleteFramebuffers(1, ref Handles[index].FrameBufferObject);
				Handles[index].DepthBufferObject = 0;
				return;
			}

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			Handles[index].Initialized = true;
		}

		public void Destroy(int handle)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id)
				return;

			lock (Lock)
			{
				if (NextFree == -1)
				{
					Handles[index].Id = -1;
					NextFree = (short)index;
				}
				else
				{
					Handles[index].Id = NextFree;
					NextFree = (short)index;
				}
			}

			if (Handles[index].Initialized)
			{
				if (Handles[index].DepthBufferObject != 0)
					GL.DeleteRenderbuffers(1, ref Handles[index].DepthBufferObject);
				GL.DeleteFramebuffers(1, ref Handles[index].FrameBufferObject);
				Handles[index].DepthBufferObject = 0;
			}

			Handles[index].Initialized = false;
		}

		public void Resize(int handle, int width, int height)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || Handles[index].DepthBufferObject == 0)
				return;

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handles[index].FrameBufferObject);
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handles[index].DepthBufferObject);
			GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

			// The render target can now be used
			Handles[index].Initialized = true;
		}

		public int GetOpenGLHande(int handle, out DrawBuffersEnum[] drawBuffer)
		{
			int index, id;
			ExtractHandle(handle, out index, out id);

			if (id == -1 || Handles[index].Id != id || !Handles[index].Initialized)
			{
				drawBuffer = null;
				return 0;
			}

			drawBuffer = Handles[index].DrawBuffers;
			return Handles[index].FrameBufferObject;
		}

		struct RenderTargetData
		{
			public short Id;
			public bool Initialized;

			public int FrameBufferObject;
			public int DepthBufferObject;
			public bool SharedDepth;
			public DrawBuffersEnum[] DrawBuffers;
		}
	}
}
