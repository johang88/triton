using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

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

		private bool Disposed = false;
		private readonly Action<Action> AddToWorkQueue;

		public RenderSystem(OpenTK.Platform.IWindowInfo windowInfo, Action<Action> addToWorkQueue)
		{
			if (windowInfo == null)
				throw new ArgumentNullException("windowInfo");
			if (addToWorkQueue == null)
				throw new ArgumentNullException("addToWorkQueue");

			AddToWorkQueue = addToWorkQueue;

			WindowInfo = windowInfo;
			var graphicsMode = new GraphicsMode(32, 16, 0, 0);

			Context = new GraphicsContext(graphicsMode, WindowInfo, 3, 0, GraphicsContextFlags.ForwardCompatible);
			Context.MakeCurrent(WindowInfo);
			Context.LoadAll();

			TextureManager = new Textures.TextureManager();
			MeshManager = new Meshes.MeshManager();
			ShaderManager = new Shaders.ShaderManager();
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

			Context.Dispose();

			Disposed = true;
		}

		public int CreateTexture(int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			var handle = TextureManager.Create();
			SetTextureData(handle, width, height, data, format, internalFormat, type);

			return handle;
		}

		public int CreateTexture(int width, int height, IntPtr data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			var handle = TextureManager.Create();
			SetTextureData(handle, width, height, data, format, internalFormat, type);

			return handle;
		}

		public void DestroyTexture(int handle)
		{
			TextureManager.Destroy(handle);
		}

		public void SetTextureData(int handle, int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			AddToWorkQueue(() =>
			{
				TextureManager.SetPixelData(handle, width, height, data, format, internalFormat, type);
			});
		}

		public void SetTextureData(int handle, int width, int height, IntPtr data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type)
		{
			AddToWorkQueue(() =>
			{
				TextureManager.SetPixelData(handle, width, height, data, format, internalFormat, type);
			});
		}

		public void BindTexture(int handle, int textureUnit)
		{
			var openGLHandle = TextureManager.GetOpenGLHande(handle);

			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, openGLHandle);
		}

		public void UnbindTexture(int textureUnit)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public int CreateMesh(int triangleCount, byte[] vertexData, byte[] indexData)
		{
			var handle = MeshManager.Create();
			SetMeshData(handle, triangleCount, vertexData, indexData);

			return handle;
		}

		public void DestroyMesh(int handle)
		{
			MeshManager.Destroy(handle);
		}

		public void SetMeshData(int mesh, int triangleCount, byte[] vertexData, byte[] indexData)
		{
			AddToWorkQueue(() =>
			{
				MeshManager.SetData(mesh, triangleCount, vertexData, indexData);
			});
		}

		public void RenderMesh(int handle)
		{
			int triangleCount, vertexArrayObjectId, indexBufferId;

			MeshManager.GetMeshData(handle, out triangleCount, out vertexArrayObjectId, out indexBufferId);
			if (triangleCount <= 0)
				return;

			GL.BindVertexArray(vertexArrayObjectId);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
			GL.DrawElements(BeginMode.Triangles, triangleCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
		}

		public void SwapBuffers()
		{
			Context.SwapBuffers();
		}

		public int CreateShader(string vertexShaderSource, string fragmentShaderSource, string[] attribs, Action<int, string> errorCallback)
		{
			var handle = ShaderManager.Create();
			SetShaderData(handle, vertexShaderSource, fragmentShaderSource, attribs, errorCallback);

			return handle;
		}

		public void DestroyShader(int handle)
		{
			ShaderManager.Destroy(handle);
		}

		public void SetShaderData(int handle, string vertexShaderSource, string fragmentShaderSource, string[] attribs, Action<int, string> errorCallback)
		{
			AddToWorkQueue(() =>
			{
				string errors;
				if (!ShaderManager.SetShaderData(handle, vertexShaderSource, fragmentShaderSource, attribs, out errors))
				{
					errorCallback(handle, errors);
				}
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

		public void SetUniform(int handle, ref OpenTK.Vector2 value)
		{
			GL.Uniform2(handle, ref value);
		}

		public void SetUniform(int handle, ref OpenTK.Vector3 value)
		{
			GL.Uniform3(handle, ref value);
		}

		public void SetUniform(int handle, ref OpenTK.Vector4 value)
		{
			GL.Uniform4(handle, ref value);
		}

		public void SetUniform(int handle, ref OpenTK.Matrix4 value)
		{
			GL.UniformMatrix4(handle, false, ref value);
		}

		public void Clear(OpenTK.Vector4 clearColor, bool depth)
		{
			GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);
			var mask = ClearBufferMask.ColorBufferBit;
			if (depth)
				mask |= ClearBufferMask.DepthBufferBit;

			GL.Clear(mask);
		}
	}
}
