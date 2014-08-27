using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer
{
	/// <summary>
	/// Core render system, a thin wrapper over OpenGL with some basic resource management functions.
	/// 
	/// All resources are exposed as integer handles, invalid or expired (unloaded) handles can be used 
	/// without any issues. If anything is wrong with the handle then a default resource will be used instead.
	/// 
	/// A single OpenGL context is owned and managed by this class, resource transfer to opengl is done in a seperate work 
	/// queue exposed through a single addToWorkQueue method, sent to the class in the constructor.
	/// Important! The work queue has to be processed on the same thread that created the RenderSystem instance. Ie the thread
	/// that owns the OpenGL context.
	/// 
	/// The work queue is only used so that the resource loading functions can be called from a background loading thread and so that 
	/// the opengl upload is done on the correct thread.
	/// 
	/// </summary>
	public class RenderSystem : IDisposable
	{
		private readonly GraphicsContext Context;
		private readonly OpenTK.Platform.IWindowInfo WindowInfo;

		private readonly Textures.TextureManager TextureManager;
		private readonly Meshes.MeshManager MeshManager;
		private readonly Meshes.BufferManager BufferManager;
		private readonly Shaders.ShaderManager ShaderManager;
		private readonly RenderTargets.RenderTargetManager RenderTargetManager;
		private readonly RenderStates.RenderStateManager RenderStateManager;
		private readonly Samplers.SamplerManager SamplerManager;

		private bool Disposed = false;
		private readonly Action<Action> AddToWorkQueue;

		public delegate void OnLoadedCallback(int handle, bool success, string errors);

		public RenderSystem(OpenTK.Platform.IWindowInfo windowInfo, Action<Action> addToWorkQueue)
		{
			if (windowInfo == null)
				throw new ArgumentNullException("windowInfo");
			if (addToWorkQueue == null)
				throw new ArgumentNullException("addToWorkQueue");

			AddToWorkQueue = addToWorkQueue;

			WindowInfo = windowInfo;
			var graphicsMode = new GraphicsMode(32, 24, 0, 0);

			Context = new GraphicsContext(graphicsMode, WindowInfo, 3, 0, GraphicsContextFlags.ForwardCompatible);
			Context.MakeCurrent(WindowInfo);
			Context.LoadAll();

			Common.Log.WriteLine("OpenGL Context initialized");
			Common.Log.WriteLine(" - Color format: {0}", Context.GraphicsMode.ColorFormat);
			Common.Log.WriteLine(" - Depth: {0}", Context.GraphicsMode.Depth);
			Common.Log.WriteLine(" - FSAA Samples: {0}", Context.GraphicsMode.Samples);

			TextureManager = new Textures.TextureManager();
			BufferManager = new Meshes.BufferManager();
			MeshManager = new Meshes.MeshManager(BufferManager);
			ShaderManager = new Shaders.ShaderManager();
			RenderTargetManager = new RenderTargets.RenderTargetManager();
			RenderStateManager = new RenderStates.RenderStateManager();
			SamplerManager = new Samplers.SamplerManager();

			var initialRenderState = RenderStateManager.CreateRenderState(false, true, true, BlendingFactorSrc.Zero, BlendingFactorDest.One, CullFaceMode.Back, true, DepthFunction.Less);
			RenderStateManager.ApplyRenderState(initialRenderState);

			GL.FrontFace(FrontFaceDirection.Ccw);
			GL.Enable(EnableCap.TextureCubeMapSeamless);
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

			SamplerManager.Dispose();
			TextureManager.Dispose();
			MeshManager.Dispose();
			BufferManager.Dispose();
			ShaderManager.Dispose();
			RenderTargetManager.Dispose();

			Context.Dispose();

			Disposed = true;
		}

		public int CreateTexture(int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type, OnLoadedCallback loadedCallback)
		{
			var handle = TextureManager.Create();
			SetTextureData(handle, width, height, data, format, internalFormat, type, loadedCallback);

			return handle;
		}

		public void DestroyTexture(int handle)
		{
			AddToWorkQueue(() => TextureManager.Destroy(handle));
		}

		public int CreateFromDDS(byte[] data, OnLoadedCallback loadedCallback)
		{
			var handle = TextureManager.Create();

			Action loadAction = () =>
			{
				TextureManager.LoadDDS(handle, data);

				if (loadedCallback != null)
					loadedCallback(handle, true, "");
			};

			AddToWorkQueue(loadAction);

			return handle;
		}

		public void SetTextureData(int handle, int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type, OnLoadedCallback loadedCallback)
		{
			Action loadAction = () =>
			{
				TextureManager.SetPixelData(handle, TextureTarget.Texture2D, width, height, data, format, internalFormat, type);

				if (loadedCallback != null)
					loadedCallback(handle, true, "");
			};

			AddToWorkQueue(loadAction);
		}

		public void BindTexture(int handle, int textureUnit)
		{
			TextureTarget target;
			var openGLHandle = TextureManager.GetOpenGLHande(handle, out target);

			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture((OpenTK.Graphics.OpenGL.TextureTarget)(int)target, openGLHandle);
		}

		public void GenreateMips(int handle)
		{
			TextureTarget target;
			var openGLHandle = TextureManager.GetOpenGLHande(handle, out target);

			GL.BindTexture((OpenTK.Graphics.OpenGL.TextureTarget)(int)target, openGLHandle);
			GL.GenerateMipmap((GenerateMipmapTarget)(int)target);
		}

		public int CreateBuffer(BufferTarget target, VertexFormat vertexFormat = null)
		{
			return BufferManager.Create(target, vertexFormat);
		}

		public void DestroyBuffer(int handle)
		{
			AddToWorkQueue(() => BufferManager.Destroy(handle));
		}

		public void SetBufferData<T>(int handle, T[] data, bool stream)
			where T : struct
		{
			AddToWorkQueue(() =>
			{
				BufferManager.SetData(handle, data, stream);
			});
		}

		public void SetBufferDataDirect(int handle, IntPtr length, IntPtr data, bool stream)
		{
			GL.BindVertexArray(0);
			BufferManager.SetDataDirect(handle, length, data, stream);
		}

		public int CreateMesh(int triangleCount, int vertexBuffer, int indexBuffer, OnLoadedCallback loadedCallback)
		{
			var handle = MeshManager.Create();

			AddToWorkQueue(() =>
			{
				MeshManager.Initialize(handle, triangleCount, vertexBuffer, indexBuffer);

				if (loadedCallback != null)
					loadedCallback(handle, true, "");
			});

			return handle;
		}

		public void DestroyMesh(int handle)
		{
			AddToWorkQueue(() => 
			{
				MeshManager.Destroy(handle);
			});
		}

		public void SetMeshDataDirect(int handle, int triangleCount, IntPtr vertexDataLength, IntPtr indexDataLength, IntPtr vertexData, IntPtr indexData, bool stream)
		{
			int vertexBufferId, indexBufferId;
			MeshManager.GetMeshData(handle, out vertexBufferId, out indexBufferId);

			GL.BindVertexArray(0);
			BufferManager.SetDataDirect(vertexBufferId, vertexDataLength, vertexData, stream);
			BufferManager.SetDataDirect(indexBufferId, indexDataLength, indexData, stream);

			MeshManager.SetTriangleCount(handle, triangleCount);
		}

		public void SetIndexBuffer(int handle, int indexBufferHandle)
		{
			AddToWorkQueue(() =>
			{
				MeshManager.SetIndexBuffer(handle, indexBufferHandle);
			});
		}

		public void MeshSetTriangleCount(int handle, int triangleCount, bool queue)
		{
			if (queue)
			{
				AddToWorkQueue(() => MeshManager.SetTriangleCount(handle, triangleCount));
			}
			else
			{
				MeshManager.SetTriangleCount(handle, triangleCount);
			}
		}

		public void RenderMesh(int handle)
		{
			int triangleCount, vertexArrayObjectId;

			MeshManager.GetRenderData(handle, out triangleCount, out vertexArrayObjectId);
			if (triangleCount <= 0)
				return;

			GL.BindVertexArray(vertexArrayObjectId);
			GL.DrawElements(PrimitiveType.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);

			RenderSystem.CheckGLError();
		}

		public void RenderMeshInstanced(int handle, int instanceCount)
		{
			int triangleCount, vertexArrayObjectId;

			MeshManager.GetRenderData(handle, out triangleCount, out vertexArrayObjectId);
			if (triangleCount <= 0)
				return;

			GL.BindVertexArray(vertexArrayObjectId);
			GL.DrawElementsInstanced(PrimitiveType.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, IntPtr.Zero, instanceCount);
			
			RenderSystem.CheckGLError();
		}

		public void BeginScene(int renderTargetHandle, int width, int height)
		{
			BindRenderTarget(renderTargetHandle);

			GL.Viewport(0, 0, width, height);
		}

		public void SwapBuffers()
		{
			Context.SwapBuffers();
		}

		public int CreateShader(string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource, OnLoadedCallback loadedCallback)
		{
			var handle = ShaderManager.Create();
			SetShaderData(handle, vertexShaderSource, fragmentShaderSource, geometryShaderSource, loadedCallback);

			return handle;
		}

		public void DestroyShader(int handle)
		{
			AddToWorkQueue(() => ShaderManager.Destroy(handle));
		}

		public void SetShaderData(int handle, string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource, OnLoadedCallback loadedCallback)
		{
			AddToWorkQueue(() =>
			{
				string errors;
				bool success = ShaderManager.SetShaderData(handle, vertexShaderSource, fragmentShaderSource, geometryShaderSource, out errors);

				if (loadedCallback != null)
					loadedCallback(handle, success, errors);
			});
		}

		public void BindShader(int handle)
		{
			var programHandle = ShaderManager.GetOpenGLHande(handle);
			GL.UseProgram(programHandle);
		}

		public int GetUniformLocation(int handle, string name)
		{
			var programHandle = ShaderManager.GetOpenGLHande(handle);
			return GL.GetUniformLocation(programHandle, name);
		}

		public Dictionary<Common.HashedString, int> GetUniforms(int handle)
		{
			return ShaderManager.GetUniforms(handle);
		}

		public void SetUniformFloat(int handle, float value)
		{
			GL.Uniform1(handle, value);
		}

		public void SetUniformInt(int handle, int value)
		{
			GL.Uniform1(handle, value);
		}

		public void SetUniformVector2(int handle, int count, ref float value)
		{
			GL.Uniform2(handle, count, ref value);
		}

		public void SetUniformMatrix4(int handle, int count, ref float value)
		{
			GL.UniformMatrix4(handle, count, false, ref value);
		}

		public void SetUniformVector3(int handle, int count, ref float value)
		{
			GL.Uniform3(handle, count, ref value);
		}

		public void SetUniformVector4(int handle, int count, ref float value)
		{
			GL.Uniform4(handle, count, ref value);
		}

		public void Clear(Triton.Vector4 clearColor, bool depth)
		{
			GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
			var mask = ClearBufferMask.ColorBufferBit;
			if (depth)
			{
				GL.DepthMask(true);
				mask |= ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit;
			}

			GL.Clear(mask);
		}

		public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less)
		{
			return RenderStateManager.CreateRenderState(enableAlphaBlend, enableDepthWrite, enableDepthTest, src, dest, cullFaceMode, enableCullFace, depthFunction);
		}

		public void SetRenderState(int id)
		{
			RenderStateManager.ApplyRenderState(id);
		}

		public int CreateRenderTarget(RenderTargets.Definition definition, out int[] textureHandles, OnLoadedCallback loadedCallback)
		{
			var internalTextureHandles = new List<int>();

			foreach (var attachment in definition.Attachments)
			{
				if (attachment.AttachmentPoint == RenderTargets.Definition.AttachmentPoint.Color || definition.RenderDepthToTexture)
				{
					var textureHandle = TextureManager.Create();
					internalTextureHandles.Add(textureHandle);
					attachment.TextureHandle = textureHandle;
				}
			}

			textureHandles = internalTextureHandles.ToArray();

			var renderTargetHandle = RenderTargetManager.Create();

			AddToWorkQueue(() =>
			{
				foreach (var attachment in definition.Attachments)
				{
					if (attachment.AttachmentPoint == RenderTargets.Definition.AttachmentPoint.Color || definition.RenderDepthToTexture)
					{
						var target = TextureTarget.Texture2D;

						if (definition.RenderToCubeMap)
						{
							target = TextureTarget.TextureCubeMap;
						}

						TextureManager.SetPixelData(attachment.TextureHandle, target, definition.Width, definition.Height, null, attachment.PixelFormat, attachment.PixelInternalFormat, attachment.PixelType, attachment.MipMaps);
						// Replace with gl handle
						attachment.TextureHandle = TextureManager.GetOpenGLHande(attachment.TextureHandle);
					}
				}

				// Init render target
				RenderTargetManager.Init(renderTargetHandle, definition);

				if (loadedCallback != null)
					loadedCallback(renderTargetHandle, true, "");
			});

			return renderTargetHandle;
		}

		public void DestroyRenderTarget(int handle)
		{
			AddToWorkQueue(() => RenderTargetManager.Destroy(handle));
		}

		public void BindRenderTarget(int handle)
		{
			DrawBuffersEnum[] drawBuffers;
			var openGLHandle = RenderTargetManager.GetOpenGLHande(handle, out drawBuffers);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, openGLHandle);

			if (drawBuffers == null)
				GL.DrawBuffer(DrawBufferMode.Back);
			else
				GL.DrawBuffers(drawBuffers.Length, drawBuffers);

			CheckGLError();
		}

		public int CreateSampler(Dictionary<SamplerParameterName, int> settings)
		{
			var handle = SamplerManager.Create();

			AddToWorkQueue(() =>
			{
				SamplerManager.Init(handle, settings);
			});

			return handle;
		}

		public void BindSampler(int textureUnit, int handle)
		{
			int sampler = SamplerManager.GetOpenGLHande(handle);
			GL.BindSampler(textureUnit, sampler);
		}

		internal static void CheckGLError()
		{
			ErrorCode error;
			while ((error = GL.GetError()) != ErrorCode.NoError)
			{
				throw new Exception(string.Format("OpenGL error {0}", error));
			}
		}
	}
}
