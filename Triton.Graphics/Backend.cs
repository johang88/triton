using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using Triton.Common;
using Triton.Renderer;

namespace Triton.Graphics
{
	/// <summary>
	/// Provides an interface to the render system. The actual render commands are processed in a seperate thread.
	/// The command stream is double buffered and this whole class is thread safe.
	/// 
	/// Note that only core rendering functionality is provided in this class. Any sorting, culling or other processing
	/// has to be done before invoking this class. As usual it is recommended that you keep the number of rendering commands
	/// executed each frame to a minimum and that you try to batch as much as possible.
	/// 
	/// The BeginInstance method is used to batch meshes with the same material together and to keep state changes to a minimum.
	/// 
	/// Example of rendering one frame
	///		BeginScene
	///			BeginPass renderTarget passSettings
	///				BeginInstance drawState
	///					BindShaderVariable shaderVar value
	///					DrawMesh mesh
	///				EndInstance
	///			EndPass
	///			BeginPass renderTarget
	///			EndPass
	///		EndScene
	///		
	/// Internally commands are encoded using a custom opcode format with variable instruction length.
	/// The instructions are encoded in byte buffers and double buffering is used to so that it is possible to
	/// write and read to the buffers at the same time.
	/// </summary>
	public class Backend : IDisposable
	{
		public INativeWindow Window { get; private set; }

		internal Triton.Renderer.RenderSystem RenderSystem { get; private set; }

		private CommandBuffer PrimaryBuffer = new CommandBuffer();
		private CommandBuffer SecondaryBuffer = new CommandBuffer();

		private readonly Semaphore DoubleBufferSynchronizer = new Semaphore(1, 1);

		private readonly ConcurrentQueue<Action> ProcessQueue = new ConcurrentQueue<Action>();

		private readonly ResourceManager ResourceManager;

		private bool IsExiting = false;
		public bool Disposed { get; private set; }
		private System.Diagnostics.Stopwatch Watch;

		public System.Drawing.Rectangle WindowBounds { get { return Window.Bounds; } }

		public bool HasFocus { get { return Window.Focused; } }
		public bool CursorVisible { get; set; }

		public Backend(ResourceManager resourceManager, int width, int height, string title, bool fullscreen)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;

			var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);

			// Create the main rendering window
			Window = new NativeWindow(width, height, title, fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);
			Window.Visible = true;
			Window.Closing += Window_Closing;

			// Setup the render system
			RenderSystem = new Renderer.RenderSystem(Window.WindowInfo, ProcessQueue.Enqueue);
			Watch = new System.Diagnostics.Stopwatch();
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

			RenderSystem.Dispose();
			Window.Dispose();

			Disposed = true;
		}

		void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			IsExiting = true;
		}

		/// <summary>
		/// Process and render a single frame
		/// The command buffer has to be written and swapped before anything is rendered.
		/// 
		/// This function will also process any tasks that has to execute on the render thread.
		/// </summary>
		/// <returns>True if the render window is still open, false otherwise</returns>
		public bool Process()
		{
			Watch.Start();

			Window.CursorVisible = CursorVisible;

			// The renderer can be disposed manually so we check here first, just in case
			if (!Window.Exists || IsExiting)
				return false;

			Window.ProcessEvents();

			// Check again in case the window got closed in the processing
			if (!Window.Exists || IsExiting)
				return false;

			// We process any resources first so that they are always ready before rendering the next frame
			while (!ProcessQueue.IsEmpty)
			{
				Action workItem;
				if (ProcessQueue.TryDequeue(out workItem))
					workItem();
			}

			// Process the rendering stream, do not swap buffers if no rendering commands have been sent, ie if the stream is still at position 0
			// We do not allow the buffers to be swapped while the stream is being processed
			if (SecondaryBuffer.Stream.Position != 0)
			{
				DoubleBufferSynchronizer.WaitOne();

				ExecuteCommandStream();

				RenderSystem.SwapBuffers();

				SecondaryBuffer.Stream.Position = 0;
				DoubleBufferSynchronizer.Release();
			}

			Watch.Restart();
			return true;
		}

		void ExecuteCommandStream()
		{
			// We never clear the stream so there can be a lot of crap after the written position, 
			// this means that we cant use Stream.Length
			var length = SecondaryBuffer.Stream.Position;

			SecondaryBuffer.Stream.Position = 0;
			var reader = SecondaryBuffer.Reader;

			while (SecondaryBuffer.Stream.Position < length)
			{
				var cmd = (OpCode)reader.ReadByte();

				switch (cmd)
				{
					case OpCode.BeginPass:
						{
							var renderTargetHandle = reader.ReadInt32();

							var width = reader.ReadInt32();
							var height = reader.ReadInt32();

							RenderSystem.BeginScene(renderTargetHandle, width, height);

							var color = reader.ReadVector4();
							var clearDepth = reader.ReadBoolean();

							RenderSystem.Clear(color, clearDepth);
						}
						break;
					case OpCode.ChangeRenderTarget:
						{
							var renderTargetHandle = reader.ReadInt32();

							RenderSystem.BeginScene(renderTargetHandle, reader.ReadInt32(), reader.ReadInt32());
						}
						break;
					case OpCode.EndPass:
						break;
					case OpCode.BeginInstance:
						{
							var shaderHandle = reader.ReadInt32();
							RenderSystem.BindShader(shaderHandle);

							var numTextures = reader.ReadInt32();

							for (var i = 0; i < numTextures; i++)
							{
								RenderSystem.BindTexture(reader.ReadInt32(), i);
							}

							var enableAlphaBlend = reader.ReadBoolean();
							var enableDepthWrite = reader.ReadBoolean();
							var enableDepthTest = reader.ReadBoolean();
							var src = (BlendingFactorSrc)reader.ReadInt32();
							var dest = (BlendingFactorDest)reader.ReadInt32();
							var cullFaceMode = (CullFaceMode)reader.ReadInt32();
							var depthFunction = (DepthFunction)reader.ReadInt32();
							var enableCullFace = reader.ReadBoolean();

							RenderSystem.SetRenderStates(enableAlphaBlend, enableDepthWrite, enableDepthTest, src, dest, cullFaceMode, enableCullFace, depthFunction);
						}
						break;
					case OpCode.EndInstance:
						break;
					case OpCode.BindShaderVariableMatrix4:
						{
							var uniformHandle = reader.ReadInt32();

							var m = reader.ReadMatrix4();
							RenderSystem.SetUniform(uniformHandle, ref m);
						}
						break;
					case OpCode.BindShaderVariableMatrix4Array:
						{
							var uniformHandle = reader.ReadInt32();

							var count = reader.ReadInt32();
							var m = new Matrix4[count];
							for (var i = 0; i < count; i++)
								m[i] = reader.ReadMatrix4();
							RenderSystem.SetUniform(uniformHandle, ref m);
						}
						break;
					case OpCode.BindShaderVariableInt:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadInt32();
							RenderSystem.SetUniform(uniformHandle, v);
						}
						break;
					case OpCode.BindShaderVariableFloat:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadSingle();
							RenderSystem.SetUniform(uniformHandle, v);
						}
						break;
					case OpCode.BindShaderVariableVector4:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadVector4();
							RenderSystem.SetUniform(uniformHandle, ref v);
						}
						break;
					case OpCode.BindShaderVariableVector3:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadVector3();
							RenderSystem.SetUniform(uniformHandle, ref v);
						}
						break;
					case OpCode.BindShaderVariableVector3Array:
						{
							var uniformHandle = reader.ReadInt32();

							var count = reader.ReadInt32();
							var v = new Vector3[count];
							for (var i = 0; i < count; i++)
								v[i] = reader.ReadVector3();
							RenderSystem.SetUniform(uniformHandle, ref v);
						}
						break;
					case OpCode.BindShaderVariableVector2:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadVector2();
							RenderSystem.SetUniform(uniformHandle, ref v);
						}
						break;
					case OpCode.DrawMesh:
						{
							var meshHandle = reader.ReadInt32();
							RenderSystem.RenderMesh(meshHandle);
						}
						break;
				}
			}
		}

		/// <summary>
		/// Add a command to the render threads work queue
		/// These commands will be proccesed once a rendering iteration has finnished
		/// </summary>
		/// <param name="workItem"></param>
		public void AddCommandToWorkQueue(Action workItem)
		{
			ProcessQueue.Enqueue(workItem);
		}

		/// <summary>
		/// Begin a new scene, this will reset the primary commnad buffer
		/// </summary>
		public void BeginScene()
		{
			PrimaryBuffer.Stream.Position = 0;
		}

		/// <summary>
		/// Render all currently queued commands. This swaps the commands buffers 
		/// and you can start to render a new frame directly after calling this method.
		/// </summary>
		public void EndScene()
		{
			DoubleBufferSynchronizer.WaitOne();

			var tmp = SecondaryBuffer;
			SecondaryBuffer = PrimaryBuffer;
			PrimaryBuffer = tmp;

			DoubleBufferSynchronizer.Release();
		}

		public void ChangeRenderTarget(RenderTarget renderTarget)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.ChangeRenderTarget);
			if (renderTarget == null)
			{
				PrimaryBuffer.Writer.Write(0);
				PrimaryBuffer.Writer.Write(Window.Width);
				PrimaryBuffer.Writer.Write(Window.Height);
			}
			else
			{
				PrimaryBuffer.Writer.Write(renderTarget.Handle);
				PrimaryBuffer.Writer.Write(renderTarget.Width);
				PrimaryBuffer.Writer.Write(renderTarget.Height);
			}
		}

		/// <summary>
		/// Begin to render a new pass to the specified render target
		/// </summary>
		/// <param name="renderTarget"></param>
		/// <param name="clearColor"></param>
		public void BeginPass(RenderTarget renderTarget, Vector4 clearColor, bool clearDepth = true)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginPass);
			if (renderTarget == null)
			{
				PrimaryBuffer.Writer.Write(0);
				PrimaryBuffer.Writer.Write(Window.Width);
				PrimaryBuffer.Writer.Write(Window.Height);
			}
			else
			{
				PrimaryBuffer.Writer.Write(renderTarget.Handle);
				PrimaryBuffer.Writer.Write(renderTarget.Width);
				PrimaryBuffer.Writer.Write(renderTarget.Height);
			}

			PrimaryBuffer.Writer.Write(clearColor);
			PrimaryBuffer.Writer.Write(clearDepth);
		}

		/// <summary>
		/// End rendering of the current render target
		/// </summary>
		public void EndPass()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.EndPass);
		}

		/// <summary>
		/// Begin a new instance, use this to batch meshes with the same textures, shaders and materials
		/// </summary>
		/// <param name="shaderHandle"></param>
		/// <param name="textures"></param>
		public void BeginInstance(int shaderHandle, int[] textures, bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginInstance);

			PrimaryBuffer.Writer.Write(shaderHandle);
			PrimaryBuffer.Writer.Write(textures.Length);

			for (var i = 0; i < textures.Length; i++)
			{
				PrimaryBuffer.Writer.Write(textures[i]);
			}

			PrimaryBuffer.Writer.Write(enableAlphaBlend);
			PrimaryBuffer.Writer.Write(enableDepthWrite);
			PrimaryBuffer.Writer.Write(enableDepthTest);
			PrimaryBuffer.Writer.Write((int)src);
			PrimaryBuffer.Writer.Write((int)dest);
			PrimaryBuffer.Writer.Write((int)cullFaceMode);
			PrimaryBuffer.Writer.Write((int)depthFunction);
			PrimaryBuffer.Writer.Write(enableCullFace);
		}

		public void EndInstance()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.EndInstance);
		}

		/// <summary>
		/// Bind a Matrix4 value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, ref Matrix4 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableMatrix4);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(ref value);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector3[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3Array);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Length);
			for (var i = 0; i < value.Length; i++)
				PrimaryBuffer.Writer.Write(ref value[i]);
		}

		public void BindShaderVariable(int uniformHandle, ref Matrix4[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableMatrix4Array);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Length);
			for (var i = 0; i < value.Length; i++)
				PrimaryBuffer.Writer.Write(ref value[i]);
		}

		/// <summary>
		/// Bind an int value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, int value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableInt);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		/// <summary>
		/// Bind a float value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, float value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableFloat);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		/// <summary>
		/// Bind a Vector4 value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, ref Vector4 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector4);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		/// <summary>
		/// Bind a Vector3 value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, ref Vector3 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		/// <summary>
		/// Bind a Vector2 value to the current shader
		/// </summary>
		/// <param name="uniformHandle"></param>
		/// <param name="value"></param>
		public void BindShaderVariable(int uniformHandle, ref Vector2 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector2);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		/// <summary>
		/// Draw a single mesh instance.
		/// BeginInstance has to be called before calling this and all shader variables should be bound
		/// </summary>
		/// <param name="handle"></param>
		public void DrawMesh(int handle)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.DrawMesh);
			PrimaryBuffer.Writer.Write(handle);
		}

		/// <summary>
		/// Create a new render target
		/// </summary>
		/// <param name="name">Name of the render target, the texture name will be derived from this in the form '_sys/render_targets/{name}_{texture_number}</param>
		/// <param name="width">Width of the render target</param>
		/// <param name="height">Height of the render target</param>
		/// <param name="pixelFormat">Desired pixel format of the render target</param>
		/// <param name="numTargets">Numver of targets to create, useful for MRT rendering. Has to be >= 1</param>
		/// <param name="createDepthBuffer">Set to true if a depth buffer is to be created</param>
		/// <returns></returns>
		public RenderTarget CreateRenderTarget(string name, int width, int height, Renderer.PixelInternalFormat pixelFormat, int numTargets, bool createDepthBuffer, int? sharedDepthHandle = null)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");
			if (width <= 0)
				throw new ArgumentException("width <= 0");
			if (height <= 0)
				throw new ArgumentException("height <= 0");
			if (numTargets <= 0)
				throw new ArgumentException("numTargets <= 0");

			int[] textureHandles;

			var renderTarget = new RenderTarget(width, height);

			renderTarget.Handle = RenderSystem.CreateRenderTarget(width, height, pixelFormat, numTargets, createDepthBuffer, sharedDepthHandle, out textureHandles, (handle, success, errors) =>
			{
				renderTarget.IsReady = true;
			});

			var textures = new Resources.Texture[textureHandles.Length];
			for (var i = 0; i < textureHandles.Length; i++)
			{
				var texture = new Resources.Texture("_sys/render_targets/" + name + "_" + StringConverter.ToString(i), "");
				texture.Handle = textureHandles[i];
				ResourceManager.Manage(texture);

				textures[i] = texture;
			}

			renderTarget.Textures = textures;

			return renderTarget;
		}

		public BatchBuffer CreateBatchBuffer(Renderer.VertexFormat vertexFormat = null, int initialCount = 128)
		{
			return new BatchBuffer(RenderSystem, vertexFormat, initialCount);
		}

		public int CreateMesh<T, T2>(int triangleCount, Renderer.VertexFormat vertexFormat, T[] vertexData, T2[] indexData, bool stream)
			where T : struct
			where T2 : struct
		{
			return RenderSystem.CreateMesh(triangleCount, vertexFormat, vertexData, indexData, stream, null);
		}

		public void UpdateMesh<T, T2>(int handle, int triangleCount, Renderer.VertexFormat vertexFormat, T[] vertexData, T2[] indexData, bool stream)
			where T : struct
			where T2 : struct
		{
			RenderSystem.SetMeshData(handle, vertexFormat, triangleCount, vertexData, indexData, stream, null);
		}

		public Resources.Texture CreateTexture(string name, int width, int height, PixelFormat pixelFormat, PixelInternalFormat interalFormat, byte[] data)
		{
			var handle = RenderSystem.CreateTexture(width, height, data, pixelFormat, interalFormat, PixelType.UnsignedByte, null);

			var texture = new Resources.Texture(name, "");
			texture.Handle = handle;
			texture.Width = width;
			texture.Height = height;
			texture.PixelInternalFormat = interalFormat;
			texture.PixelFormat = pixelFormat;

			ResourceManager.Manage(texture);

			return texture;
		}

		public void UpdateTexture(Resources.Texture texture, byte[] data)
		{
			RenderSystem.SetTextureData(texture.Handle, texture.Width, texture.Height, data, texture.PixelFormat, texture.PixelInternalFormat, PixelType.UnsignedByte, null);
		}

		/// <summary>
		/// Op codes used in the rendering instructions
		/// All opcodes have a variable amount of parameters
		/// </summary>
		enum OpCode : byte
		{
			BeginPass,
			ChangeRenderTarget,
			EndPass,
			BeginInstance,
			EndInstance,
			BindShaderVariableMatrix4,
			BindShaderVariableMatrix4Array,
			BindShaderVariableInt,
			BindShaderVariableFloat,
			BindShaderVariableVector2,
			BindShaderVariableVector3,
			BindShaderVariableVector3Array,
			BindShaderVariableVector4,
			DrawMesh
		}

		/// <summary>
		/// Buffer class used to encode the instruction stream.
		/// Provides access to readers / writers on the byte buffer.
		/// </summary>
		class CommandBuffer
		{
			public readonly MemoryStream Stream;
			public readonly BinaryReader Reader;
			public readonly BinaryWriter Writer;

			public CommandBuffer()
			{
				Stream = new MemoryStream(8192);
				Reader = new BinaryReader(Stream);
				Writer = new BinaryWriter(Stream);
			}
		}
	}
}
