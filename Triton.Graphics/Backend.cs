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
	/// It is also possible to queue work items on the render thread using the method AddCommandToWorkQueue.
	/// The rendering thread is usually idle for some time so that's why we allow misc items to be processed, note however
	/// that it is best if this is kept to loading resources for use in the renderer itself.
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
	/// </summary>
	public class Backend : IDisposable
	{
		const long MaxTimeForMiscProcessing = 32;
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

		public Backend(ResourceManager resourceManager, int width, int height, string title, bool fullscreen)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;

			var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);

			Window = new NativeWindow(width, height, title, fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);
			Window.Visible = true;
			Window.Closing += Window_Closing;

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

			// The renderer can be disposed manually so we check here first, just in case
			if (!Window.Exists || IsExiting)
				return false;

			Window.ProcessEvents();

			// Check again in case the window got closed in the processing
			if (!Window.Exists || IsExiting)
				return false;

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

			// Do some misc processing, ie usually resource loading etc
			var miscProccessingStart = Watch.ElapsedMilliseconds;
			while (!ProcessQueue.IsEmpty && Watch.ElapsedMilliseconds - miscProccessingStart < MaxTimeForMiscProcessing)
			{
				Action workItem;
				if (ProcessQueue.TryDequeue(out workItem))
					workItem();
			}

			Watch.Restart();
			return true;
		}

		public void BackgroundLoad()
		{
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

							RenderSystem.BeginScene(renderTargetHandle, reader.ReadInt32(), reader.ReadInt32());

							var color = reader.ReadVector4();
							RenderSystem.Clear(color, true);
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
						}
						break;
					case OpCode.EndInstance:
						break;
					case OpCode.BindShaderVariableMatrix4:
						{
							var uniformHandle = reader.ReadInt32();

							var m = new Matrix4(
								reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
								reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
								reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
								reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
								);

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
							var v = reader.ReadVector4();
							RenderSystem.SetUniform(uniformHandle, ref v);
						}
						break;
					case OpCode.BindShaderVariableVector2:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadVector4();
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

		public void BeginScene()
		{
			PrimaryBuffer.Stream.Position = 0;
		}

		public void EndScene()
		{
			DoubleBufferSynchronizer.WaitOne();

			var tmp = SecondaryBuffer;
			SecondaryBuffer = PrimaryBuffer;
			PrimaryBuffer = tmp;

			DoubleBufferSynchronizer.Release();
		}

		public void BeginPass(RenderTarget renderTarget, Vector4 clearColor)
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
		}

		public void EndPass()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.EndPass);
		}

		public void BeginInstance(int shaderHandle, int[] textures)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginInstance);

			PrimaryBuffer.Writer.Write(shaderHandle);
			PrimaryBuffer.Writer.Write(textures.Length);

			for (var i = 0; i < textures.Length; i++)
			{
				PrimaryBuffer.Writer.Write(textures[i]);
			}
		}

		public void EndInstance()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.EndInstance);
		}

		public void BindShaderVariable(int uniformHandle, ref Matrix4 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableMatrix4);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Row0.X);
			PrimaryBuffer.Writer.Write(value.Row0.Y);
			PrimaryBuffer.Writer.Write(value.Row0.Z);
			PrimaryBuffer.Writer.Write(value.Row0.W);

			PrimaryBuffer.Writer.Write(value.Row1.X);
			PrimaryBuffer.Writer.Write(value.Row1.Y);
			PrimaryBuffer.Writer.Write(value.Row1.Z);
			PrimaryBuffer.Writer.Write(value.Row1.W);

			PrimaryBuffer.Writer.Write(value.Row2.X);
			PrimaryBuffer.Writer.Write(value.Row2.Y);
			PrimaryBuffer.Writer.Write(value.Row2.Z);
			PrimaryBuffer.Writer.Write(value.Row2.W);

			PrimaryBuffer.Writer.Write(value.Row3.X);
			PrimaryBuffer.Writer.Write(value.Row3.Y);
			PrimaryBuffer.Writer.Write(value.Row3.Z);
			PrimaryBuffer.Writer.Write(value.Row3.W);
		}

		public void BindShaderVariable(int uniformHandle, int value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableInt);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector4 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector4);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector3 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector2 value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector2);
			PrimaryBuffer.Writer.Write(uniformHandle);
			PrimaryBuffer.Writer.Write(value);
		}

		public void DrawMesh(int handle)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.DrawMesh);
			PrimaryBuffer.Writer.Write(handle);
		}

		public RenderTarget CreateRenderTarget(string name, int width, int height, Renderer.PixelInternalFormat pixelFormat, int numTargets, bool createDepthBuffer)
		{
			int[] textureHandles;

			var renderTarget = new RenderTarget(width, height);

			renderTarget.Handle = RenderSystem.CreateRenderTarget(width, height, pixelFormat, numTargets, createDepthBuffer, out textureHandles, (handle, success, errors) =>
			{
				renderTarget.IsReady = true;
			});

			var textures = new Resources.Texture[textureHandles.Length];
			for (var i = 0; i < textureHandles.Length; i++)
			{
				var texture = new Resources.Texture("_sys/render_targets/" + name + "_" + StringConverter.ToString(i) + ".texture", "");
				texture.IsLoaded = true;
				texture.Handle = textureHandles[i];
				ResourceManager.Manage(texture);

				textures[i] = texture;
			}

			renderTarget.Textures = textures;

			return renderTarget;
		}

		public BatchBuffer CreateBatchBuffer(int initialCount = 128)
		{
			return new BatchBuffer(RenderSystem, initialCount);
		}

		enum OpCode : byte
		{
			BeginPass,
			EndPass,
			BeginInstance,
			EndInstance,
			BindShaderVariableMatrix4,
			BindShaderVariableInt,
			BindShaderVariableVector2,
			BindShaderVariableVector3,
			BindShaderVariableVector4,
			DrawMesh
		}

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
