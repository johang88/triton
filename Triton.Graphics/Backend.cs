using OGL = OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
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
using Triton.Graphics.Resources;

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
        public Triton.Renderer.RenderSystem RenderSystem { get; private set; }

        private CommandBuffer _primaryBuffer = new CommandBuffer();
        private CommandBuffer _secondaryBuffer = new CommandBuffer();

        private Profiler _primaryProfiler;
        public Profiler SecondaryProfiler;

        private readonly Semaphore _doubleBufferSynchronizer = new Semaphore(1, 1);
        private bool _commandStreamReady = false;

        private readonly ConcurrentQueue<Action> _processQueue = new ConcurrentQueue<Action>();
        private readonly List<Action> _endOfFrameActions = new List<Action>();

        private readonly ResourceManager _resourceManager;
        private readonly ContextReference _contextReference;

        private bool _isExiting = false;
        public bool Disposed { get; private set; }
        private readonly System.Diagnostics.Stopwatch Watch;

        public float FrameTime { get; private set; }
        public float ElapsedTime { get; private set; }

        public readonly int DefaultSampler;
        public readonly int DefaultSamplerNoFiltering;
        public readonly int DefaultSamplerMipMapNearest;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public int DrawCalls = 0;

        private ShaderHotReloader _shaderHotReloader;

        public Backend(ResourceManager resourceManager, int width, int height, ContextReference contextReference)
        {
            Width = width;
            Height = height;

            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            _contextReference = contextReference;

            // Setup the render system
            RenderSystem = new Renderer.RenderSystem(_processQueue.Enqueue, contextReference);
            Watch = new System.Diagnostics.Stopwatch();

            DefaultSampler = RenderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear },
                { SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
                { SamplerParameterName.TextureMaxAnisotropyExt, 16 },
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

            _primaryProfiler = new Profiler();
            SecondaryProfiler = new Profiler();

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

            _primaryProfiler.Dispose();
            SecondaryProfiler.Dispose();

            RenderSystem.Dispose();
            Disposed = true;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isExiting = true;
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

            if (_isExiting || Disposed)
                return false;

            // We process any resources first so that they are always ready before rendering the next frame
            while (!_processQueue.IsEmpty)
            {
                if (_processQueue.TryDequeue(out var workItem))
                    workItem();
            }

            // Shader hot reloading, it's a bit ad hoc and ugly atm ... but it might work
            if (_shaderHotReloader!= null)
            {
                _shaderHotReloader.Tick();
            }

            // Process the rendering stream, do not swap buffers if no rendering commands have been sent, ie if the stream is still at position 0
            // We do not allow the buffers to be swapped while the stream is being processed
            _doubleBufferSynchronizer.WaitOne();
            if (_commandStreamReady)
            {
                _commandStreamReady = false;
                Watch.Restart();

                _primaryProfiler.Collect();

                var tmpProfiler = SecondaryProfiler;
                SecondaryProfiler = _primaryProfiler;
                _primaryProfiler = tmpProfiler;

                _primaryProfiler.Reset();

                ExecuteCommandStream();

                _contextReference.SwapBuffers();

                // We may now dispatch end of frame actions for gpu readbacks and stuff
                foreach (var action in _endOfFrameActions)
                {
                    action();
                }
                _endOfFrameActions.Clear();

                _secondaryBuffer.Stream.Position = 0;

                FrameTime = (float)Watch.Elapsed.TotalSeconds;
                ElapsedTime += FrameTime;
            }
            _doubleBufferSynchronizer.Release();

            return true;
        }

        unsafe void ExecuteCommandStream()
        {
            DrawCalls = 0;

            // We never clear the stream so there can be a lot of crap after the written position, 
            // this means that we cant use Stream.Length
            var length = _secondaryBuffer.Stream.Position;

            _secondaryBuffer.Stream.Position = 0;
            var reader = _secondaryBuffer.Reader;

            var buffer = ((MemoryStream)reader.BaseStream).GetBuffer();
            fixed (byte* p = buffer)
            {
                while (_secondaryBuffer.Stream.Position < length)
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
                                    var flags = (ClearFlags)reader.ReadByte();

                                    RenderSystem.Clear(color, flags);
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
                                    if (samplerHandle != 0)
                                        RenderSystem.BindSampler(texUnit++, samplerHandle);
                                }
                            }
                            break;
                        case OpCode.EndInstance:
                            break;
                        case OpCode.BindShaderVariableMatrix4:
                            {
                                var uniformHandle = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformMatrix4(uniformHandle, 1, m);

                                reader.BaseStream.Position += sizeof(float) * 16;
                            }
                            break;
                        case OpCode.BindShaderVariableMatrix4Array:
                            {
                                var uniformHandle = reader.ReadInt32();
                                var count = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformMatrix4(uniformHandle, count, m);

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

                                var m = (int*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformInt(uniformHandle, count, m);

                                reader.BaseStream.Position += sizeof(int) * count;
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

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformFloat(uniformHandle, count, m);

                                reader.BaseStream.Position += sizeof(float) * count;
                            }
                            break;
                        case OpCode.BindShaderVariableVector4:
                            {
                                var uniformHandle = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector4(uniformHandle, 1, m);

                                reader.BaseStream.Position += sizeof(float) * 4;
                            }
                            break;
                        case OpCode.BindShaderVariableVector3:
                            {
                                var uniformHandle = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector3(uniformHandle, 1, m);

                                reader.BaseStream.Position += sizeof(float) * 3;
                            }
                            break;
                        case OpCode.BindShaderVariableVector3Array:
                            {
                                var uniformHandle = reader.ReadInt32();
                                var count = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector3(uniformHandle, count, m);

                                reader.BaseStream.Position += sizeof(float) * 3 * count;
                            }
                            break;
                        case OpCode.BindShaderVariableVector4Array:
                            {
                                var uniformHandle = reader.ReadInt32();
                                var count = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector4(uniformHandle, count, m);

                                reader.BaseStream.Position += sizeof(float) * 4 * count;
                            }
                            break;
                        case OpCode.BindShaderVariableVector2:
                            {
                                var uniformHandle = reader.ReadInt32();

                                var m = (float*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector2(uniformHandle, 1, m);

                                reader.BaseStream.Position += sizeof(float) * 2;
                            }
                            break;
                        case OpCode.BindShaderVariableUint2:
                            {
                                var uniformHandle = reader.ReadInt32();

                                var m = (uint*)(p + reader.BaseStream.Position);
                                RenderSystem.SetUniformVector2u(uniformHandle, 1, m);

                                reader.BaseStream.Position += sizeof(uint) * 2;
                            }
                            break;
                        case OpCode.DrawMesh:
                            {
                                DrawCalls++;
                                var meshHandle = reader.ReadInt32();
                                RenderSystem.RenderMesh(meshHandle, OGL.PrimitiveType.Triangles);
                            }
                            break;
                        case OpCode.DrawMeshOffset:
                            {
                                DrawCalls++;
                                var meshHandle = reader.ReadInt32();
                                var offset = reader.ReadInt32();
                                var count = reader.ReadInt32();
                                RenderSystem.RenderMesh(meshHandle, OGL.PrimitiveType.Triangles, offset, count);
                            }
                            break;
                        case OpCode.DrawMeshTesselated:
                            {
                                DrawCalls++;
                                var meshHandle = reader.ReadInt32();
                                RenderSystem.RenderMesh(meshHandle, OGL.PrimitiveType.Patches);
                            }
                            break;
                        case OpCode.DrawMeshInstanced:
                            {
                                DrawCalls++;
                                var meshHandle = reader.ReadInt32();
                                var instanceCount = reader.ReadInt32();
                                var instanceBufferId = reader.ReadInt32();

                                RenderSystem.RenderMeshInstanced(meshHandle, instanceCount, instanceBufferId);
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

                                var vertices = (p + reader.BaseStream.Position);
                                var indices = (p + reader.BaseStream.Position + vertexLength);

                                RenderSystem.SetMeshDataDirect(meshHandle, triangleCount, (IntPtr)vertexLength, (IntPtr)indexLength, (IntPtr)vertices, (IntPtr)indices, stream);

                                reader.BaseStream.Position += vertexLength + indexLength;
                            }
                            break;
                        case OpCode.UpdateBuffer:
                            {
                                var handle = reader.ReadInt32();
                                var dataLength = reader.ReadInt32();

                                var data = (p + reader.BaseStream.Position);
                                RenderSystem.SetBufferDataDirect(handle, (IntPtr)dataLength, (IntPtr)data, true);

                                reader.BaseStream.Position += dataLength;
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
                                _primaryProfiler.Begin(name);
                            }
                            break;
                        case OpCode.ProfileEnd:
                            {
                                int name = reader.ReadInt32();
                                _primaryProfiler.End(name);
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
                        case OpCode.WireFrame:
                            {
                                var enabled = reader.ReadBoolean();

                                RenderSystem.SetWireFrameEnabled(enabled);
                            }
                            break;
                        case OpCode.Scissor:
                            {
                                var enable = reader.ReadBoolean();
                                var x = reader.ReadInt32();
                                var y = reader.ReadInt32();
                                var w = reader.ReadInt32();
                                var h = reader.ReadInt32();

                                RenderSystem.Scissor(enable, x, y, w, h);
                            }
                            break;
                        case OpCode.BindImageTexture:
                            {
                                var unit = reader.ReadInt32();
                                var texture = reader.ReadInt32();
                                var access = (OGL.TextureAccess)reader.ReadInt32();
                                var format = (OGL.SizedInternalFormat)reader.ReadInt32();

                                RenderSystem.BindImageTexture(unit, texture, access, format);
                            }
                            break;
                        case OpCode.BindBufferBase:
                            {
                                var index = reader.ReadInt32();
                                var handle = reader.ReadInt32();

                                RenderSystem.BindBufferBase(index, handle);
                            }
                            break;

                        case OpCode.BindBufferRange:
                            {
                                var index = reader.ReadInt32();
                                var handle = reader.ReadInt32();
                                var offset = reader.ReadInt32();
                                var size = reader.ReadInt32();

                                RenderSystem.BindBufferRange(index, handle, (IntPtr)offset, (IntPtr)size);
                            }
                            break;
                        case OpCode.Barrier:
                            var barrier = (OpenTK.Graphics.OpenGL.MemoryBarrierFlags)reader.ReadInt32();
                            OGL.GL.MemoryBarrier(barrier);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Begin a new scene, this will reset the primary commnad buffer
        /// </summary>
        public void BeginScene()
        {
            _primaryBuffer.Stream.Position = 0;
        }

        /// <summary>
        /// Render all currently queued commands. This swaps the commands buffers 
        /// and you can start to render a new frame directly after calling this method.
        /// </summary>
        public void EndScene()
        {
            _doubleBufferSynchronizer.WaitOne();

            var tmp = _secondaryBuffer;
            _secondaryBuffer = _primaryBuffer;
            _primaryBuffer = tmp;
            _commandStreamReady = true;

            _doubleBufferSynchronizer.Release();
        }

        public void ChangeRenderTarget(RenderTarget renderTarget)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.ChangeRenderTarget);
            if (renderTarget == null)
            {
                _primaryBuffer.Writer.Write(0);
                _primaryBuffer.Writer.Write(Width);
                _primaryBuffer.Writer.Write(Height);
            }
            else
            {
                _primaryBuffer.Writer.Write(renderTarget.Handle);
                _primaryBuffer.Writer.Write(renderTarget.Width);
                _primaryBuffer.Writer.Write(renderTarget.Height);
            }
        }

        /// <summary>
        /// Begin to render a new pass to the specified render target
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="clearColor"></param>
        public void BeginPass(RenderTarget renderTarget)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BeginPass);
            if (renderTarget == null)
            {
                _primaryBuffer.Writer.Write(0);
                _primaryBuffer.Writer.Write(Width);
                _primaryBuffer.Writer.Write(Height);
            }
            else
            {
                _primaryBuffer.Writer.Write(renderTarget.Handle);
                _primaryBuffer.Writer.Write(renderTarget.Width);
                _primaryBuffer.Writer.Write(renderTarget.Height);
            }

            _primaryBuffer.Writer.Write(false);
        }

        /// <summary>
        /// Begin to render a new pass to the specified render target
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="clearColor"></param>
        public void BeginPass(RenderTarget renderTarget, Vector4 clearColor, ClearFlags clearFlags = ClearFlags.Depth | ClearFlags.Color)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BeginPass);
            if (renderTarget == null)
            {
                _primaryBuffer.Writer.Write(0);
                _primaryBuffer.Writer.Write(Width);
                _primaryBuffer.Writer.Write(Height);
            }
            else
            {
                _primaryBuffer.Writer.Write(renderTarget.Handle);
                _primaryBuffer.Writer.Write(renderTarget.Width);
                _primaryBuffer.Writer.Write(renderTarget.Height);
            }

            _primaryBuffer.Writer.Write(true);
            _primaryBuffer.Writer.Write(clearColor);
            _primaryBuffer.Writer.Write((byte)clearFlags);
        }

        /// <summary>
        /// End rendering of the current render target
        /// </summary>
        public void EndPass()
        {
            _primaryBuffer.Writer.Write((byte)OpCode.EndPass);
        }

        /// <summary>
        /// Begin a new instance, use this to batch meshes with the same textures, shaders and materials
        /// </summary>
        /// <param name="shaderHandle"></param>
        /// <param name="textures"></param>
        public void BeginInstance(int shaderHandle, int[] textures, int[] samplers, int renderStateId = 0)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BeginInstance);

            _primaryBuffer.Writer.Write(shaderHandle);
            if (textures != null)
            {
                _primaryBuffer.Writer.Write(textures.Length);

                for (var i = 0; i < textures.Length; i++)
                {
                    _primaryBuffer.Writer.Write(textures[i]);
                }
            }
            else
            {
                _primaryBuffer.Writer.Write(0);
            }

            _primaryBuffer.Writer.Write(renderStateId);

            if (samplers != null)
            {
                _primaryBuffer.Writer.Write(samplers.Length);
                foreach (var sampler in samplers)
                {
                    _primaryBuffer.Writer.Write(sampler);
                }
            }
            else
            {
                _primaryBuffer.Writer.Write(0);
            }
        }

        public void EndInstance()
        {
            // no op
            //PrimaryBuffer.Writer.Write((byte)OpCode.EndInstance);
        }

        /// <summary>
        /// Bind a Matrix4 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, ref Matrix4 value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableMatrix4);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(ref value);
        }

        public void BindShaderVariable(int uniformHandle, uint v0, uint v1)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableUint2);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(v0);
            _primaryBuffer.Writer.Write(v1);
        }

        public void BindShaderVariable(int uniformHandle, ref int[] value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableIntArray);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(value.Length);
            for (var i = 0; i < value.Length; i++)
                _primaryBuffer.Writer.Write(value[i]);
        }

        public void BindShaderVariable(int uniformHandle, ref float[] value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableFloatArray);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(value.Length);
            for (var i = 0; i < value.Length; i++)
                _primaryBuffer.Writer.Write(value[i]);
        }

        public void BindShaderVariable(int uniformHandle, ref Vector3[] value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3Array);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(value.Length);
            for (var i = 0; i < value.Length; i++)
                _primaryBuffer.Writer.Write(ref value[i]);
        }

        public void BindShaderVariable(int uniformHandle, ref Vector4[] value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector4Array);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(value.Length);
            for (var i = 0; i < value.Length; i++)
                _primaryBuffer.Writer.Write(ref value[i]);
        }

        public void BindShaderVariable(int uniformHandle, ref Matrix4[] value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableMatrix4Array);
            _primaryBuffer.Writer.Write(uniformHandle);

            _primaryBuffer.Writer.Write(value.Length);
            for (var i = 0; i < value.Length; i++)
                _primaryBuffer.Writer.Write(ref value[i]);
        }

        /// <summary>
        /// Bind an int value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, int value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableInt);
            _primaryBuffer.Writer.Write(uniformHandle);
            _primaryBuffer.Writer.Write(value);
        }

        /// <summary>
        /// Bind a float value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, float value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableFloat);
            _primaryBuffer.Writer.Write(uniformHandle);
            _primaryBuffer.Writer.Write(value);
        }

        /// <summary>
        /// Bind a Vector4 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, ref Vector4 value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector4);
            _primaryBuffer.Writer.Write(uniformHandle);
            _primaryBuffer.Writer.Write(value);
        }

        /// <summary>
        /// Bind a Vector3 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, ref Vector3 value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector3);
            _primaryBuffer.Writer.Write(uniformHandle);
            _primaryBuffer.Writer.Write(value);
        }

        /// <summary>
        /// Bind a Vector2 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        public void BindShaderVariable(int uniformHandle, ref Vector2 value)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindShaderVariableVector2);
            _primaryBuffer.Writer.Write(uniformHandle);
            _primaryBuffer.Writer.Write(value);
        }

        /// <summary>
        /// Draw a single mesh instance.
        /// BeginInstance has to be called before calling this and all shader variables should be bound
        /// </summary>
        /// <param name="handle"></param>
        public void DrawMesh(int handle)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.DrawMesh);
            _primaryBuffer.Writer.Write(handle);
        }

        public void DrawMeshOffset(int handle, int offset, int count)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.DrawMeshOffset);
            _primaryBuffer.Writer.Write(handle);
            _primaryBuffer.Writer.Write(offset);
            _primaryBuffer.Writer.Write(count);
        }

        public void DrawMeshTesselated(int handle)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.DrawMeshTesselated);
            _primaryBuffer.Writer.Write(handle);
        }

        /// <summary>
        /// Draw an instanced mesh.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="instanceCount"></param>
        public void DrawMeshInstanced(int handle, int instanceCount, int instanceBufferId)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.DrawMeshInstanced);
            _primaryBuffer.Writer.Write(handle);
            _primaryBuffer.Writer.Write(instanceCount);
            _primaryBuffer.Writer.Write(instanceBufferId);
        }

        /// <summary>
        /// Uploads a mesh inline in the command stream, UpdateMesh() is preferred if it not neccecary to change a mesh while rendering.
        /// </summary>
        public void UpdateMeshInline(int handle, int triangleCount, int vertexCount, int indexCount, float[] vertexData, int[] indexData, bool stream)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.UpdateMesh);
            _primaryBuffer.Writer.Write(handle);
            _primaryBuffer.Writer.Write(triangleCount);
            _primaryBuffer.Writer.Write(stream);

            _primaryBuffer.Writer.Write(vertexCount);
            _primaryBuffer.Writer.Write(indexCount);

            for (var i = 0; i < vertexCount; i++)
            {
                _primaryBuffer.Writer.Write(vertexData[i]);
            }

            for (var i = 0; i < indexCount; i++)
            {
                _primaryBuffer.Writer.Write(indexData[i]);
            }
        }

        public void UpdateBufferInline(int handle, int dataCount, Matrix4[] data)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.UpdateBuffer);
            _primaryBuffer.Writer.Write(handle);
            _primaryBuffer.Writer.Write(dataCount * sizeof(float) * 16);
            for (var i = 0; i < dataCount; i++)
            {
                _primaryBuffer.Writer.Write(ref data[i]);
            }
        }

        public unsafe void UpdateBufferInline(int handle, int size, byte* data)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.UpdateBuffer);
            _primaryBuffer.Writer.Write(handle);
            _primaryBuffer.Writer.Write(size);

            var buffer = ((MemoryStream)_primaryBuffer.Writer.BaseStream).GetBuffer();
            var offset = _primaryBuffer.Writer.BaseStream.Position;

            fixed (byte* d = buffer)
            {
                for (var i = 0; i < size; i++)
                {
                    *(d + i + offset) = *(data + i);
                }
            }

            _primaryBuffer.Writer.Seek(size, SeekOrigin.Current);
        }

        public void GenerateMips(int textureHandle)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.GenerateMips);
            _primaryBuffer.Writer.Write(textureHandle);
        }

        public void ProfileBeginSection(Common.HashedString name)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.ProfileBegin);
            _primaryBuffer.Writer.Write((int)name);
        }

        public void ProfileEndSection(Common.HashedString name)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.ProfileEnd);
            _primaryBuffer.Writer.Write((int)name);
        }

        public void DispatchCompute(int numGroupsX, int numGroupsY, int numGroupsZ)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.DispatchCompute);
            _primaryBuffer.Writer.Write(numGroupsX);
            _primaryBuffer.Writer.Write(numGroupsY);
            _primaryBuffer.Writer.Write(numGroupsZ);
        }

        public void Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags barrier)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.Barrier);
            _primaryBuffer.Writer.Write((int)barrier);
        }

        public void WireFrame(bool enable)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.WireFrame);
            _primaryBuffer.Writer.Write(enable);
        }

        public void Scissor(bool enable, int x, int y, int w, int h)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.Scissor);
            _primaryBuffer.Writer.Write(enable);
            _primaryBuffer.Writer.Write(x);
            _primaryBuffer.Writer.Write(y);
            _primaryBuffer.Writer.Write(w);
            _primaryBuffer.Writer.Write(h);
        }

        public void BindImageTexture(int unit, int texture, OGL.TextureAccess access, OGL.SizedInternalFormat format)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindImageTexture);
            _primaryBuffer.Writer.Write(unit);
            _primaryBuffer.Writer.Write(texture);
            _primaryBuffer.Writer.Write((int)access);
            _primaryBuffer.Writer.Write((int)format);
        }

        public void BindBufferBase(int index, int handle)
        {
            _primaryBuffer.Writer.Write((byte)OpCode.BindBufferBase);
            _primaryBuffer.Writer.Write(index);
            _primaryBuffer.Writer.Write(handle);
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
                var texture = new Resources.Texture(this)
                {
                    Handle = textureHandles[i],
                    Width = definition.Width,
                    Height = definition.Height
                };

                _resourceManager.Manage("_sys/render_targets/" + name + "_" + StringConverter.ToString(i), texture);

                textures[i] = texture;
            }

            renderTarget.Textures = textures;

            return renderTarget;
        }

        public void ResizeRenderTarget(RenderTarget renderTarget, int width, int height)
        {
            foreach (var texture in renderTarget.Textures)
            {
                texture.Width = width;
                texture.Height = height;
            }

            renderTarget.Width = width;
            renderTarget.Height = height;

            RenderSystem.ResizeRenderTarget(renderTarget.Handle, width, height);
        }

        public BatchBuffer CreateBatchBuffer(Renderer.VertexFormat vertexFormat = null, int initialCount = 128)
        {
            return new BatchBuffer(RenderSystem, vertexFormat, initialCount);
        }

        public Resources.Texture CreateTexture(string name, int width, int height, PixelFormat pixelFormat, PixelInternalFormat interalFormat, PixelType pixelType, byte[] data, bool mipmap)
        {
            var handle = RenderSystem.CreateTexture(width, height, data, pixelFormat, interalFormat, pixelType, mipmap, null);

            var texture = new Resources.Texture(this)
            {
                Handle = handle,
                Width = width,
                Height = height,
                PixelInternalFormat = interalFormat,
                PixelFormat = pixelFormat
            };

            _resourceManager.Manage(name, texture);

            return texture;
        }

        public void UpdateTexture(Resources.Texture texture, bool mipmap, byte[] data)
        {
            RenderSystem.SetTextureData(texture.Handle, texture.Width, texture.Height, data, texture.PixelFormat, texture.PixelInternalFormat, PixelType.UnsignedByte, mipmap, null);
        }

        public SpriteBatch CreateSpriteBatch()
        {
            return new SpriteBatch(this, RenderSystem, _resourceManager);
        }

        public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less, bool wireFrame = false)
        {
            return RenderSystem.CreateRenderState(enableAlphaBlend, enableDepthWrite, enableDepthTest, src, dest, cullFaceMode, enableCullFace, depthFunction, wireFrame);
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Op codes used in the rendering instructions
        /// All opcodes have a variable amount of parameters
        /// </summary>
        enum OpCode : int
        {
            BeginPass,
            ChangeRenderTarget,
            EndPass,
            BeginInstance,
            EndInstance,
            BindShaderVariableUint2,
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
            DrawMeshOffset,
            DrawMeshInstanced,
            GenerateMips,
            ProfileBegin,
            ProfileEnd,
            DispatchCompute,
            WireFrame,
            DrawMeshTesselated,
            UpdateBuffer,
            Scissor,
            BindImageTexture,
            BindBufferBase,
            BindBufferRange,
            Barrier
        }

        public void ScheduleForEndOfFrame(Action action)
        {
            _endOfFrameActions.Add(action);
        }

        internal void ConfigureShaderHotReloading(ShaderSerializer loader, ShaderHotReloadConfig shaderHotReloadConfig)
        {
            if (!shaderHotReloadConfig.Enable)
                return;

            _shaderHotReloader = new ShaderHotReloader(loader, shaderHotReloadConfig.Path, shaderHotReloadConfig.BasePath);
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
                Stream = new MemoryStream(1024 * 1024 * 16); // 16mb default size
                Reader = new BinaryReader(Stream);
                Writer = new BinaryWriter(Stream);
            }
        }
    }
}
