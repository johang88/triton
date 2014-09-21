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
using System.Runtime.InteropServices;

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

		private Profiler PrimaryProfiler = new Profiler();
		public Profiler SecondaryProfiler = new Profiler();

		private readonly Semaphore DoubleBufferSynchronizer = new Semaphore(1, 1);

		private readonly ConcurrentQueue<Action> ProcessQueue = new ConcurrentQueue<Action>();

		private readonly ResourceManager ResourceManager;

		private bool IsExiting = false;
		public bool Disposed { get; private set; }
		private System.Diagnostics.Stopwatch Watch;

		public System.Drawing.Rectangle WindowBounds { get { return Window.Bounds; } }

		public bool HasFocus { get { return Window.Focused; } }
		public bool CursorVisible { get; set; }

		public float FrameTime { get; private set; }
		public float ElapsedTime { get; private set; }

		public readonly int DefaultSampler;
		public readonly int DefaultSamplerNoFiltering;
		public readonly int DefaultSamplerMipMapNearest;

		public int Width { get; private set; }
		public int Height { get; private set; }

		public int WindowWidth { get { return Window.Width; } }
		public int WindowHeight { get { return Window.Height; } }

		public Backend(ResourceManager resourceManager, int width, int height, float resolutionScale, string title, bool fullscreen)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;

			var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);

			// Create the main rendering window
			Window = new NativeWindow(width, height, title, fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);

			Width = (int)(WindowWidth * resolutionScale);
			Height = (int)(WindowHeight * resolutionScale);

			Window.Visible = true;
			Window.Closing += Window_Closing;

			Log.WriteLine("Window created @ {0}x{1} {2}", Window.Width, Window.Height, fullscreen ? "fullscreen" : "windowed");

			// Setup the render system
			RenderSystem = new Renderer.RenderSystem(Window.WindowInfo, ProcessQueue.Enqueue);
			Watch = new System.Diagnostics.Stopwatch();

			DefaultSampler = RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
				{ SamplerParameterName.TextureMaxAnisotropyExt, 8 },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.Repeat },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.Repeat },
				{ SamplerParameterName.TextureWrapR, (int)TextureWrapMode.Repeat }
			});

			DefaultSamplerNoFiltering = RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			DefaultSamplerMipMapNearest = RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
			{
				{ SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest },
				{ SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest },
				{ SamplerParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge },
				{ SamplerParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge }
			});

			ElapsedTime = 0;
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

				PrimaryProfiler.Reset();
				ExecuteCommandStream();

				RenderSystem.SwapBuffers();

				SecondaryBuffer.Stream.Position = 0;
				DoubleBufferSynchronizer.Release();
			}

			FrameTime = (float)Watch.Elapsed.TotalSeconds;
			ElapsedTime += FrameTime;
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

							var clear = reader.ReadBoolean();

							if (clear)
							{
								var color = reader.ReadVector4();
								var clearDepth = reader.ReadBoolean();

								RenderSystem.Clear(color, clearDepth);
							}
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
								var textureHandle = reader.ReadInt32();
								if (textureHandle != 0)
									RenderSystem.BindTexture(textureHandle, i);
							}

							var renderStateId = reader.ReadInt32();

							RenderSystem.SetRenderState(renderStateId);

							var numSamplers = reader.ReadInt32();
							var texUnit = 0;
							for (int i = 0; i < numSamplers; i++)
							{
								var samplerHandle = reader.ReadInt32();
								RenderSystem.BindSampler(texUnit++, samplerHandle);
							}
						}
						break;
					case OpCode.EndInstance:
						break;
					case OpCode.BindShaderVariableMatrix4:
						{
							var uniformHandle = reader.ReadInt32();

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformMatrix4(uniformHandle, 1, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 16;
						}
						break;
					case OpCode.BindShaderVariableMatrix4Array:
						{
							var uniformHandle = reader.ReadInt32();
							var count = reader.ReadInt32();
	
							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformMatrix4(uniformHandle, count, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 16 * count;
						}
						break;
					case OpCode.BindShaderVariableInt:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadInt32();
							RenderSystem.SetUniformInt(uniformHandle, v);
						}
						break;
					case OpCode.BindShaderVariableIntArray:
						{
							var uniformHandle = reader.ReadInt32();
							var count = reader.ReadInt32();
	
							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (int*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformInt(uniformHandle, count, m);
								}
							}

							reader.BaseStream.Position += sizeof(int) * 4 * count;
						}
						break;
					case OpCode.BindShaderVariableFloat:
						{
							var uniformHandle = reader.ReadInt32();
							var v = reader.ReadSingle();
							RenderSystem.SetUniformFloat(uniformHandle, v);
						}
						break;
					case OpCode.BindShaderVariableFloatArray:
						{
							var uniformHandle = reader.ReadInt32();
							var count = reader.ReadInt32();

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformFloat(uniformHandle, count, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 4 * count;
						}
						break;
					case OpCode.BindShaderVariableVector4:
						{
							var uniformHandle = reader.ReadInt32();

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformVector4(uniformHandle, 1, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 4;
						}
						break;
					case OpCode.BindShaderVariableVector3:
						{
							var uniformHandle = reader.ReadInt32();
							
							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformVector3(uniformHandle, 1, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 3;
						}
						break;
					case OpCode.BindShaderVariableVector3Array:
						{
							var uniformHandle = reader.ReadInt32();
							var count = reader.ReadInt32();

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformVector3(uniformHandle, count, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 3 * count;
						}
						break;
					case OpCode.BindShaderVariableVector4Array:
						{
							var uniformHandle = reader.ReadInt32();
							var count = reader.ReadInt32();

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformVector4(uniformHandle, count, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 4 * count;
						}
						break;
					case OpCode.BindShaderVariableVector2:
						{
							var uniformHandle = reader.ReadInt32();
							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var m = (float*)(p + reader.BaseStream.Position);
									RenderSystem.SetUniformVector2(uniformHandle, 1, m);
								}
							}

							reader.BaseStream.Position += sizeof(float) * 2;
						}
						break;
					case OpCode.DrawMesh:
						{
							var meshHandle = reader.ReadInt32();
							RenderSystem.RenderMesh(meshHandle);
						}
						break;
					case OpCode.DrawMeshInstanced:
						{
							var meshHandle = reader.ReadInt32();
							var instanceCount = reader.ReadInt32();
							RenderSystem.RenderMeshInstanced(meshHandle, instanceCount);
						}
						break;
					case OpCode.UpdateMesh:
						{
							var meshHandle = reader.ReadInt32();
							var triangleCount = reader.ReadInt32();
							var stream = reader.ReadBoolean();

							var vertexCount = reader.ReadInt32();
							var indexCount = reader.ReadInt32();

							var vertexLength = vertexCount * sizeof(float);
							var indexLength = indexCount * sizeof(int);

							var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();

							unsafe
							{
								fixed (byte* p = buffer)
								{
									var vertices = (p + reader.BaseStream.Position);
									var indices = (p + reader.BaseStream.Position + vertexLength);

									RenderSystem.SetMeshDataDirect(meshHandle, triangleCount, (IntPtr)vertexLength, (IntPtr)indexLength, (IntPtr)vertices, (IntPtr)indices, stream);
								}
							}

							reader.BaseStream.Position += vertexLength + indexLength;
						}
						break;
					case OpCode.GenerateMips:
						{
							var textureHandle = reader.ReadInt32();
							RenderSystem.GenreateMips(textureHandle);
						}
						break;
					case OpCode.ProfileBegin:
						{
							int name = reader.ReadInt32();
							PrimaryProfiler.Begin(name);
						}
						break;
					case OpCode.ProfileEnd:
						{
							int name = reader.ReadInt32();
							OpenTK.Graphics.OpenGL.GL.Finish();
							PrimaryProfiler.End(name);
						}
						break;
					case OpCode.DispatchCompute:
						{
							var numGroupsX = reader.ReadInt32();
							var numGroupsY = reader.ReadInt32();
							var numGroupsZ = reader.ReadInt32();

							RenderSystem.DispatchCompute(numGroupsX, numGroupsY, numGroupsZ);
						}
						break;
				}
			}
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

			var tmpProfiler = SecondaryProfiler;
			SecondaryProfiler = PrimaryProfiler;
			PrimaryProfiler = tmpProfiler;

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
		public void BeginPass(RenderTarget renderTarget)
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

			PrimaryBuffer.Writer.Write(false);
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

			PrimaryBuffer.Writer.Write(true);
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
		public void BeginInstance(int shaderHandle, int[] textures, int[] samplers, int renderStateId = 0)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BeginInstance);

			PrimaryBuffer.Writer.Write(shaderHandle);
			PrimaryBuffer.Writer.Write(textures.Length);

			for (var i = 0; i < textures.Length; i++)
			{
				PrimaryBuffer.Writer.Write(textures[i]);
			}

			PrimaryBuffer.Writer.Write(renderStateId);

			if (samplers != null)
			{
				PrimaryBuffer.Writer.Write(samplers.Length);
				foreach (var sampler in samplers)
				{
					PrimaryBuffer.Writer.Write(sampler);
				}
			}
			else
			{
				PrimaryBuffer.Writer.Write(0);
			}
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

		public void BindShaderVariable(int uniformHandle, ref int[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableIntArray);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Length);
			for (var i = 0; i < value.Length; i++)
				PrimaryBuffer.Writer.Write(value[i]);
		}

		public void BindShaderVariable(int uniformHandle, ref float[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableFloatArray);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Length);
			for (var i = 0; i < value.Length; i++)
				PrimaryBuffer.Writer.Write(value[i]);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector3[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3Array);
			PrimaryBuffer.Writer.Write(uniformHandle);

			PrimaryBuffer.Writer.Write(value.Length);
			for (var i = 0; i < value.Length; i++)
				PrimaryBuffer.Writer.Write(ref value[i]);
		}

		public void BindShaderVariable(int uniformHandle, ref Vector4[] value)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector4Array);
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
		/// Draw an instanced mesh.
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="instanceCount"></param>
		public void DrawMeshInstanced(int handle, int instanceCount)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.DrawMeshInstanced);
			PrimaryBuffer.Writer.Write(handle);
			PrimaryBuffer.Writer.Write(instanceCount);
		}

		/// <summary>
		/// Uploads a mesh inline in the command stream, UpdateMesh() is preferred if it not neccecary to change a mesh while rendering.
		/// </summary>
		public void UpdateMeshInline(int handle, int triangleCount, int vertexCount, int indexCount, float[] vertexData, int[] indexData, bool stream)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.UpdateMesh);
			PrimaryBuffer.Writer.Write(handle);
			PrimaryBuffer.Writer.Write(triangleCount);
			PrimaryBuffer.Writer.Write(stream);

			PrimaryBuffer.Writer.Write(vertexCount);
			PrimaryBuffer.Writer.Write(indexCount);

			for (var i = 0; i < vertexCount; i++)
			{
				PrimaryBuffer.Writer.Write(vertexData[i]);
			}

			for (var i = 0; i < indexCount; i++)
			{
				PrimaryBuffer.Writer.Write(indexData[i]);
			}
		}

		public void GenerateMips(int textureHandle)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.GenerateMips);
			PrimaryBuffer.Writer.Write(textureHandle);
		}

		public void ProfileBeginSection(Common.HashedString name)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.ProfileBegin);
			PrimaryBuffer.Writer.Write((int)name);
		}

		public void ProfileEndSection(Common.HashedString name)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.ProfileEnd);
			PrimaryBuffer.Writer.Write((int)name);
		}

		public void DispatchCompute(int numGroupsX, int numGroupsY, int numGroupsZ)
		{
			PrimaryBuffer.Writer.Write((byte)OpCode.DispatchCompute);
			PrimaryBuffer.Writer.Write(numGroupsX);
			PrimaryBuffer.Writer.Write(numGroupsY);
			PrimaryBuffer.Writer.Write(numGroupsZ);
		}

		public RenderTarget CreateRenderTarget(string name, Renderer.RenderTargets.Definition definition)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");
			if (definition == null)
				throw new ArgumentNullException("definition");

			int[] textureHandles;

			var renderTarget = new RenderTarget(definition.Width, definition.Height);

			renderTarget.Handle = RenderSystem.CreateRenderTarget(definition, out textureHandles, (handle, success, errors) =>
			{
				renderTarget.IsReady = true;
			});

			var textures = new Resources.Texture[textureHandles.Length];
			for (var i = 0; i < textureHandles.Length; i++)
			{
				var texture = new Resources.Texture("_sys/render_targets/" + name + "_" + StringConverter.ToString(i), "");
				texture.Handle = textureHandles[i];
				texture.Width = definition.Width;
				texture.Height = definition.Height;

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

		public Resources.Texture CreateTexture(string name, int width, int height, PixelFormat pixelFormat, PixelInternalFormat interalFormat, PixelType pixelType, byte[] data, bool mipmap)
		{
			var handle = RenderSystem.CreateTexture(width, height, data, pixelFormat, interalFormat, pixelType, mipmap, null);

			var texture = new Resources.Texture(name, "");
			texture.Handle = handle;
			texture.Width = width;
			texture.Height = height;
			texture.PixelInternalFormat = interalFormat;
			texture.PixelFormat = pixelFormat;

			ResourceManager.Manage(texture);

			return texture;
		}

		public void UpdateTexture(Resources.Texture texture, bool mipmap, byte[] data)
		{
			RenderSystem.SetTextureData(texture.Handle, texture.Width, texture.Height, data, texture.PixelFormat, texture.PixelInternalFormat, PixelType.UnsignedByte, mipmap, null);
		}

		public SpriteBatch CreateSpriteBatch()
		{
			return new SpriteBatch(this, RenderSystem, ResourceManager);
		}

		public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less)
		{
			return RenderSystem.CreateRenderState(enableAlphaBlend, enableDepthWrite, enableDepthTest, src, dest, cullFaceMode, enableCullFace, depthFunction);
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
			BindShaderVariableIntArray,
			BindShaderVariableFloat,
			BindShaderVariableFloatArray,
			BindShaderVariableVector2,
			BindShaderVariableVector3,
			BindShaderVariableVector3Array,
			BindShaderVariableVector4,
			BindShaderVariableVector4Array,
			UpdateMesh,
			DrawMesh,
			DrawMeshInstanced,
			GenerateMips,
			ProfileBegin,
			ProfileEnd,
			DispatchCompute
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
