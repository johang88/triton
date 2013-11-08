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
		private readonly Shaders.ShaderManager ShaderManager;
		private readonly RenderTargets.RenderTargetManager RenderTargetManager;

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

			TextureManager = new Textures.TextureManager();
			MeshManager = new Meshes.MeshManager();
			ShaderManager = new Shaders.ShaderManager();
			RenderTargetManager = new RenderTargets.RenderTargetManager();

			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask(true);

			GL.Enable(EnableCap.CullFace);
			GL.FrontFace(FrontFaceDirection.Ccw);
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

			TextureManager.Dispose();
			MeshManager.Dispose();
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
			TextureManager.Destroy(handle);
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

			if (Context.IsCurrent)
				loadAction();
			else
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

			if (Context.IsCurrent)
				loadAction();
			else
				AddToWorkQueue(loadAction);
		}

		public void BindTexture(int handle, int textureUnit)
		{
			TextureTarget target;
			var openGLHandle = TextureManager.GetOpenGLHande(handle, out target);

			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture((OpenTK.Graphics.OpenGL.TextureTarget)(int)target, openGLHandle);
		}

		public int CreateMesh<T, T2>(int triangleCount, VertexFormat vertexFormat, T[] vertexData, T2[] indexData, bool stream, OnLoadedCallback loadedCallback)
			where T : struct
			where T2 : struct
		{
			var handle = MeshManager.Create();
			SetMeshData(handle, vertexFormat, triangleCount, vertexData, indexData, stream, loadedCallback);

			return handle;
		}

		public void DestroyMesh(int handle)
		{
			MeshManager.Destroy(handle);
		}

		public void SetMeshData<T, T2>(int handle, VertexFormat vertexFormat, int triangleCount, T[] vertexData, T2[] indexData, bool stream, OnLoadedCallback loadedCallback)
			where T : struct
			where T2 : struct
		{
			AddToWorkQueue(() =>
			{
				MeshManager.SetData(handle, vertexFormat, triangleCount, vertexData, indexData, stream);

				if (loadedCallback != null)
					loadedCallback(handle, true, "");
			});
		}

		public void SetMeshData(int handle, VertexFormat vertexFormat, int triangleCount, float[] vertexData, int[] indexData, bool stream, OnLoadedCallback loadedCallback)
		{
			AddToWorkQueue(() =>
			{
				MeshManager.SetData(handle, vertexFormat, triangleCount, vertexData, indexData, stream);

				if (loadedCallback != null)
					loadedCallback(handle, true, "");
			});
		}

		public void RenderMesh(int handle)
		{
			int triangleCount, vertexArrayObjectId, indexBufferId;

			MeshManager.GetMeshData(handle, out triangleCount, out vertexArrayObjectId, out indexBufferId);
			if (triangleCount <= 0)
				return;

			GL.BindVertexArray(vertexArrayObjectId);
			GL.DrawElements(BeginMode.Triangles, triangleCount * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);
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

		public int CreateShader(string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource, string[] attribs, string[] fragDataLocations, OnLoadedCallback loadedCallback)
		{
			var handle = ShaderManager.Create();
			SetShaderData(handle, vertexShaderSource, fragmentShaderSource, geometryShaderSource, attribs, fragDataLocations, loadedCallback);

			return handle;
		}

		public void DestroyShader(int handle)
		{
			ShaderManager.Destroy(handle);
		}

		public void SetShaderData(int handle, string vertexShaderSource, string fragmentShaderSource, string geometryShaderSource, string[] attribs, string[] fragDataLocations, OnLoadedCallback loadedCallback)
		{
			AddToWorkQueue(() =>
			{
				string errors;
				bool success = ShaderManager.SetShaderData(handle, vertexShaderSource, fragmentShaderSource, geometryShaderSource, attribs, fragDataLocations, out errors);

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

		public void SetUniform(int handle, float value)
		{
			GL.Uniform1(handle, value);
		}

		public void SetUniform(int handle, int value)
		{
			GL.Uniform1(handle, value);
		}

		public void SetUniform(int handle, ref Vector2 value)
		{
			GL.Uniform2(handle, 1, ref value.X);
		}

		public void SetUniform(int handle, ref Vector3 value)
		{
			GL.Uniform3(handle, 1, ref value.X);
		}

		public void SetUniform(int handle, ref Vector4 value)
		{
			GL.Uniform4(handle, 1, ref value.X);
		}

		public void SetUniform(int handle, ref Matrix4 value)
		{
			GL.UniformMatrix4(handle, 1, false, ref value.Row0.X);
		}

		public void SetUniform(int handle, ref Matrix4[] value)
		{
			GL.UniformMatrix4(handle, value.Length, false, ref value[0].Row0.X);
		}

		public void SetUniform(int handle, ref Vector3[] value)
		{
			GL.Uniform3(handle, value.Length, ref value[0].X);
		}

		public void SetUniform(int handle, ref Vector4[] value)
		{
			GL.Uniform4(handle, value.Length, ref value[0].X);
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

		public void SetRenderStates(bool enableAlphaBlend, bool enableDepthWrite, bool enableDepthTest, BlendingFactorSrc src, BlendingFactorDest dest, CullFaceMode cullFaceMode, bool enableCullFace, DepthFunction depthFunction = DepthFunction.Less)
		{
			if (enableAlphaBlend)
				GL.Enable(EnableCap.Blend);
			else
				GL.Disable(EnableCap.Blend);

			if (enableDepthTest)
				GL.Enable(EnableCap.DepthTest);
			else
				GL.Disable(EnableCap.DepthTest);

			GL.DepthMask(enableDepthWrite);

			if (enableCullFace)
				GL.Enable(EnableCap.CullFace);
			else
				GL.Disable(EnableCap.CullFace);

			GL.BlendFunc((OpenTK.Graphics.OpenGL.BlendingFactorSrc)(int)src, (OpenTK.Graphics.OpenGL.BlendingFactorDest)(int)dest);
			GL.CullFace((OpenTK.Graphics.OpenGL.CullFaceMode)(int)cullFaceMode);

			GL.DepthFunc((OpenTK.Graphics.OpenGL.DepthFunction)(int)depthFunction);
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
						TextureManager.SetPixelData(attachment.TextureHandle, TextureTarget.Texture2D, definition.Width, definition.Height, null, attachment.PixelFormat, attachment.PixelInternalFormat, attachment.PixelType, false, false);
						// Replace with gl handle
						attachment.TextureHandle = TextureManager.GetOpenGLHande(attachment.TextureHandle);

						if (attachment.AttachmentPoint == RenderTargets.Definition.AttachmentPoint.Depth)
						{
							GL.BindTexture(OGL.TextureTarget.Texture2D, attachment.TextureHandle);
							GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
							GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
							GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
							GL.TexParameter(OGL.TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture);
						}
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
			RenderTargetManager.Destroy(handle);
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
