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

		public Triton.Renderer.RenderSystem RenderSystem { get; private set; }
		private bool Disposed = false;
		private readonly Thread RenderThread;

		private CommandBuffer PrimaryBuffer = new CommandBuffer();
		private CommandBuffer SecondaryBuffer = new CommandBuffer();

		private readonly Semaphore DoubleBufferSynchronizer = new Semaphore(1, 1);

		private readonly ConcurrentQueue<Action> ProcessQueue = new ConcurrentQueue<Action>();

		public Action OnShuttingDown = null;

		public Backend(int width, int height, string title, bool fullscreen, Action onReady = null)
		{
			RenderThread = new Thread(() =>
			{
				var graphicsMode = new GraphicsMode(new ColorFormat(32), 214, 0, 0);

				Window = new NativeWindow(width, height, title, fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);
				Window.Visible = true;
				RenderSystem = new Renderer.RenderSystem(Window.WindowInfo, ProcessQueue.Enqueue);

				if (onReady != null)
					onReady();

				RenderLoop();
			});
			RenderThread.Start();
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

		void RenderLoop()
		{
			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

			while (Window.Exists)
			{
				Window.ProcessEvents();

				if (SecondaryBuffer.Stream.Position == 0)
					continue;

				DoubleBufferSynchronizer.WaitOne();

				ExecuteCommandStream();

				RenderSystem.SwapBuffers();

				SecondaryBuffer.Stream.Position = 0;
				DoubleBufferSynchronizer.Release();

				// Do some misc processing, ie usually resource loading etc
				var miscProccessingStart = watch.ElapsedMilliseconds;
				while (!ProcessQueue.IsEmpty && watch.ElapsedMilliseconds - miscProccessingStart < MaxTimeForMiscProcessing)
				{
					Action workItem;
					if (ProcessQueue.TryDequeue(out workItem))
						workItem();
				}

				watch.Restart();
			}

			if (OnShuttingDown != null)
				OnShuttingDown();
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
						RenderSystem.Clear(Vector4.One, true);
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

		public void BeginPass()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginPass);
		}

		public void EndPass()
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.EndPass);
		}

		public void BeginInstance(int shaderHandle, int[] textures)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginInstance);

			PrimaryBuffer.Writer.Write(shaderHandle);
			PrimaryBuffer.Writer.Write((byte)textures.Length);

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

			PrimaryBuffer.Writer.Write(value.M11);
			PrimaryBuffer.Writer.Write(value.M12);
			PrimaryBuffer.Writer.Write(value.M13);
			PrimaryBuffer.Writer.Write(value.M14);

			PrimaryBuffer.Writer.Write(value.M21);
			PrimaryBuffer.Writer.Write(value.M22);
			PrimaryBuffer.Writer.Write(value.M23);
			PrimaryBuffer.Writer.Write(value.M24);

			PrimaryBuffer.Writer.Write(value.M31);
			PrimaryBuffer.Writer.Write(value.M32);
			PrimaryBuffer.Writer.Write(value.M33);
			PrimaryBuffer.Writer.Write(value.M34);

			PrimaryBuffer.Writer.Write(value.M41);
			PrimaryBuffer.Writer.Write(value.M42);
			PrimaryBuffer.Writer.Write(value.M43);
			PrimaryBuffer.Writer.Write(value.M44);
		}

		public void DrawMesh(int handle)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.DrawMesh);
			PrimaryBuffer.Writer.Write(handle);
		}

		enum OpCode : byte
		{
			BeginPass,
			EndPass,
			BeginInstance,
			EndInstance,
			BindShaderVariableMatrix4,
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
