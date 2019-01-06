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
using Triton.Renderer;
using System.Runtime.InteropServices;
using Triton.Graphics.Resources;
using Triton.Resources;
using Triton.Utility;
using Triton.IO;
using System.Runtime.CompilerServices;
using Triton.Renderer.Meshes;

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
    public partial class Backend : IDisposable
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
            if (Disposed)
                return;

            _primaryBuffer.Dispose();
            _secondaryBuffer.Dispose();

            if (!isDisposing)
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
            if (_shaderHotReloader != null)
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

                _secondaryBuffer.Length = 0;

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
            var length = _secondaryBuffer.Length;

            var ptr = (byte*)_secondaryBuffer.Buffer;
            var position = 0L;

            while (position < length)
            {
                var header = *(PacketHeader*)ptr;
                ptr += sizeof(PacketHeader);
                position += sizeof(PacketHeader);

                switch (header.OpCode)
                {
                    case OpCode.BeginPass:
                        {
                            var packet = (PacketBeginPass*)(ptr);

                            RenderSystem.BeginScene(packet->Handle, packet->Width, packet->Height);
                            if (packet->ClearFlags != ClearFlags.None)
                            {
                                RenderSystem.Clear(packet->ClearColor, packet->ClearFlags);
                            }
                        }
                        break;
                    case OpCode.BeginPass2:
                        {
                            var packet = (PacketBeginPass2*)(ptr);

                            RenderSystem.BeginScene(packet->Handle, packet->X, packet->Y, packet->W, packet->H);
                            if (packet->ClearFlags != ClearFlags.None)
                            {
                                RenderSystem.Scissor(true, packet->X, packet->Y, packet->W, packet->H);
                                RenderSystem.Clear(packet->ClearColor, packet->ClearFlags);
                                RenderSystem.Scissor(false, packet->X, packet->Y, packet->W, packet->H);
                            }
                        }
                        break;
                    case OpCode.BeginInstance:
                        {
                            var packet = *(PacketBeginInstance*)(ptr);
                            var data = (int*)(ptr + sizeof(PacketBeginInstance));

                            RenderSystem.BindShader(packet.ShaderHandle);
                            RenderSystem.SetRenderState(packet.RenderStateId);

                            for (var i = 0; i < packet.NumberOfTextures; i++)
                            {
                                var textureHandle = *data++;
                                if (textureHandle != 0)
                                {
                                    RenderSystem.BindTexture(textureHandle, i);
                                }
                            }

                            var texUnit = 0;
                            for (int i = 0; i < packet.NumberOfSamplers; i++)
                            {
                                var samplerHandle = *data++;
                                if (samplerHandle != 0)
                                {
                                    RenderSystem.BindSampler(texUnit++, samplerHandle);
                                }
                            }
                        }
                        break;
                    case OpCode.EndInstance:
                        break;
                    case OpCode.BindShaderVariableMatrix4:
                        {
                            var packet = (PacketBindShaderVariableMatrix4*)(ptr);
                            RenderSystem.SetUniformMatrix4(packet->UniformHandle, 1, &packet->Value.Row0.X);
                        }
                        break;
                    case OpCode.BindShaderVariableMatrix4Array:
                        {
                            var packet = *(PacketBindShaderVariableArray*)(ptr);
                            var m = (float*)(ptr + sizeof(PacketBindShaderVariableArray));
                            RenderSystem.SetUniformMatrix4(packet.UniformHandle, packet.Count, m);
                        }
                        break;
                    case OpCode.BindShaderVariableInt:
                        {
                            var packet = (PacketBindShaderVariableInt*)(ptr);
                            RenderSystem.SetUniformInt(packet->UniformHandle, packet->X);
                        }
                        break;
                    case OpCode.BindShaderVariableIntArray:
                        {
                            var packet = *(PacketBindShaderVariableArray*)(ptr);
                            var m = (int*)(ptr + sizeof(PacketBindShaderVariableArray));
                            RenderSystem.SetUniformInt(packet.UniformHandle, packet.Count, m);
                        }
                        break;
                    case OpCode.BindShaderVariableFloat:
                        {
                            var packet = (PacketBindShaderVariableFloat*)(ptr);
                            RenderSystem.SetUniformFloat(packet->UniformHandle, packet->X);
                        }
                        break;
                    case OpCode.BindShaderVariableFloatArray:
                        {
                            var packet = *(PacketBindShaderVariableArray*)(ptr);
                            var m = (float*)(ptr + sizeof(PacketBindShaderVariableArray));
                            RenderSystem.SetUniformFloat(packet.UniformHandle, packet.Count, m);
                        }
                        break;
                    case OpCode.BindShaderVariableVector4:
                        {
                            var packet = (PacketBindShaderVariableVector4*)(ptr);
                            RenderSystem.SetUniformVector4(packet->UniformHandle, 1, &packet->Value.X);
                        }
                        break;
                    case OpCode.BindShaderVariableVector3:
                        {
                            var packet = (PacketBindShaderVariableVector3*)(ptr);
                            RenderSystem.SetUniformVector3(packet->UniformHandle, 1, &packet->Value.X);
                        }
                        break;
                    case OpCode.BindShaderVariableVector3Array:
                        {
                            var packet = *(PacketBindShaderVariableArray*)(ptr);
                            var m = (float*)(ptr + sizeof(PacketBindShaderVariableArray));
                            RenderSystem.SetUniformVector3(packet.UniformHandle, packet.Count, m);
                        }
                        break;
                    case OpCode.BindShaderVariableVector2:
                        {
                            var packet = (PacketBindShaderVariableVector2*)(ptr);
                            RenderSystem.SetUniformVector2(packet->UniformHandle, 1, &packet->Value.X);
                        }
                        break;
                    case OpCode.BindShaderVariableUint2:
                        {
                            var packet = (PacketBindShaderVariableUint2*)(ptr);
                            RenderSystem.SetUniformVector2u(packet->UniformHandle, 1, &packet->X);
                        }
                        break;
                    case OpCode.DrawMesh:
                        {
                            DrawCalls++;
                            var packet = *(PacketDrawMesh*)(ptr);
                            RenderSystem.RenderMesh(packet.MeshHandle, OGL.PrimitiveType.Triangles);
                        }
                        break;
                    case OpCode.DrawMeshMulti:
                        {
                            DrawCalls++;
                            var packet = *(PacketDrawMeshMulti*)(ptr);
                            var meshIndices = (DrawMeshMultiData*)(ptr + sizeof(PacketDrawMeshMulti));
                            RenderSystem.RenderMesh(meshIndices, packet.Count);
                        }
                        break;
                    case OpCode.DrawMeshOffset:
                        {
                            DrawCalls++;
                            var packet = *(PacketDrawMeshOffset*)(ptr);
                            RenderSystem.RenderMesh(packet.MeshHandle, OGL.PrimitiveType.Triangles, packet.Offset, packet.Count);
                        }
                        break;
                    case OpCode.UpdateMesh:
                        {
                            var packet = *(PacketUpdateMesh*)(ptr);

                            var vertexLength = packet.VertexCount * sizeof(float);
                            var indexLength = packet.IndexCount * sizeof(int);

                            var vertices = (ptr + sizeof(PacketUpdateMesh));
                            var indices = (ptr + sizeof(PacketUpdateMesh) + vertexLength);

                            RenderSystem.SetMeshDataDirect(packet.Handle, packet.TriangleCount, (IntPtr)vertexLength, (IntPtr)indexLength, (IntPtr)vertices, (IntPtr)indices, packet.Stream);
                        }
                        break;
                    case OpCode.UpdateBuffer:
                        {
                            var packet = *(PacketUpdateBuffer*)(ptr);

                            var data = (ptr + sizeof(PacketUpdateBuffer));
                            RenderSystem.SetBufferDataDirect(packet.Handle, (IntPtr)packet.Size, (IntPtr)data, true);
                        }
                        break;
                    case OpCode.GenerateMips:
                        {
                            var packet = *(PacketGenerateMips*)(ptr);
                            RenderSystem.GenreateMips(packet.Handle);
                        }
                        break;
                    case OpCode.ProfileBegin:
                        {
                            var packet = *(PacketProfileSection*)(ptr);
                            _primaryProfiler.Begin(packet.Name);
                        }
                        break;
                    case OpCode.ProfileEnd:
                        {
                            var packet = *(PacketProfileSection*)(ptr);
                            _primaryProfiler.End(packet.Name);
                        }
                        break;
                    case OpCode.DispatchCompute:
                        {
                            var packet = *(PacketDispatchCompute*)(ptr);
                            RenderSystem.DispatchCompute(packet.NumGroupsX, packet.NumGroupsY, packet.NumGroupsZ);
                        }
                        break;
                    case OpCode.Scissor:
                        {
                            var packet = *(PacketScissor*)(ptr);
                            RenderSystem.Scissor(packet.Enable, packet.X, packet.Y, packet.W, packet.H);
                        }
                        break;
                    case OpCode.BindImageTexture:
                        {
                            var packet = *(PacketBindImageTexture*)(ptr);
                            RenderSystem.BindImageTexture(packet.Unit, packet.TextureHandle, packet.Access, packet.Format);
                        }
                        break;
                    case OpCode.BindBufferBase:
                        {
                            var packet = *(PacketBindBufferBase*)(ptr);
                            RenderSystem.BindBufferBase(packet.Index, packet.Handle);
                        }
                        break;
                    case OpCode.Barrier:
                        {
                            var packet = *(PacketBarrier*)(ptr);
                            OGL.GL.MemoryBarrier(packet.Barrier);
                        }
                        break;
                }

                ptr += header.Size;
                position += header.Size;
            }
        }

        /// <summary>
        /// Begin a new scene, this will reset the primary commnad buffer
        /// </summary>
        public void BeginScene()
        {
            _primaryBuffer.Length = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteHeader<T>(OpCode opCode, int dataSize, out T* ptr) where T : unmanaged
        {
            var offset = _primaryBuffer.Length;

            var bodySize = sizeof(T) + dataSize;

            // Pad end of packet in order to align to IntPtr size
            // Assume that header and base pointer is already aligned
            var end = offset + sizeof(PacketHeader) + bodySize;
            var align = sizeof(IntPtr);

            var mod = end % align;
            if (mod > 0)
            {
                bodySize += (align - mod);
            }

            *(PacketHeader*)(_primaryBuffer.Buffer + offset) = new PacketHeader
            {
                OpCode = opCode,
                Size = bodySize
            };

            _primaryBuffer.Length += sizeof(PacketHeader) + bodySize;
            offset += sizeof(PacketHeader);

            ptr = (T*)(_primaryBuffer.Buffer + offset);
        }

        /// <summary>
        /// Begin to render a new pass to the specified render target
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="clearColor"></param>
        public unsafe void BeginPass(RenderTarget renderTarget)
        {
            WriteHeader<PacketBeginPass>(OpCode.BeginPass, 0, out var packet);

            packet->Handle = renderTarget?.Handle ?? 0;
            packet->Width = renderTarget?.Width ?? Width;
            packet->Height = renderTarget?.Height ?? Height;
            packet->ClearColor = Vector4.Zero;
            packet->ClearFlags = ClearFlags.None;
        }

        /// <summary>
        /// Begin to render a new pass to the specified render target
        /// </summary>
        /// <param name="renderTarget"></param>
        /// <param name="clearColor"></param>
        public unsafe void BeginPass(RenderTarget renderTarget, Vector4 clearColor, ClearFlags clearFlags = ClearFlags.Depth | ClearFlags.Color)
        {
            WriteHeader<PacketBeginPass>(OpCode.BeginPass, 0, out var packet);

            packet->Handle = renderTarget?.Handle ?? 0;
            packet->Width = renderTarget?.Width ?? Width;
            packet->Height = renderTarget?.Height ?? Height;
            packet->ClearColor = clearColor;
            packet->ClearFlags = clearFlags;
        }

        public unsafe void BeginPass(RenderTarget renderTarget, Vector4 clearColor, int x, int y, int w, int h, ClearFlags clearFlags = ClearFlags.Depth | ClearFlags.Color)
        {
            WriteHeader<PacketBeginPass2>(OpCode.BeginPass2, 0, out var packet);

            packet->Handle = renderTarget?.Handle ?? 0;
            packet->X = x;
            packet->Y = y;
            packet->W = w;
            packet->H = h;
            packet->ClearColor = clearColor;
            packet->ClearFlags = clearFlags;
        }

        /// <summary>
        /// End rendering of the current render target
        /// </summary>
        public void EndPass()
        {
            // NOP
        }

        /// <summary>
        /// Begin a new instance, use this to batch meshes with the same textures, shaders and materials
        /// </summary>
        /// <param name="shaderHandle"></param>
        /// <param name="textures"></param>
        public unsafe void BeginInstance(int shaderHandle, int[] textures, int[] samplers, int renderStateId = 0)
        {
            var dataSize = sizeof(int) * (textures.Length + samplers.Length);
            WriteHeader<PacketBeginInstance>(OpCode.BeginInstance, dataSize, out var packet);

            *packet = new PacketBeginInstance
            {
                ShaderHandle = shaderHandle,
                RenderStateId = renderStateId,
                NumberOfTextures = textures.Length,
                NumberOfSamplers = samplers.Length
            };

            var data = (int*)(packet + 1);
            for (var i = 0; i < textures.Length; i++)
            {
                *data++ = textures[i];
            }

            for (var i = 0; i < samplers.Length; i++)
            {
                *data++ = samplers[i];
            }
        }

        public void EndInstance()
        {
            // no op
        }

        /// <summary>
        /// Bind a Matrix4 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, ref Matrix4 value)
        {
            WriteHeader<PacketBindShaderVariableMatrix4>(OpCode.BindShaderVariableMatrix4, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, uint v0, uint v1)
        {
            WriteHeader<PacketBindShaderVariableUint2>(OpCode.BindShaderVariableUint2, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->X = v0;
            packet->Y = v1;
        }

        public unsafe void BindShaderVariable(int uniformHandle, ref int[] value)
        {
            WriteHeader<PacketBindShaderVariableArray>(OpCode.BindShaderVariableIntArray, sizeof(int) * value.Length, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Count = value.Length;

            var data = (int*)(packet + 1);
            for (var i = 0; i < value.Length; i++)
            {
                *data++ = value[i];
            }
        }

        public unsafe void BindShaderVariable(int uniformHandle, ref float[] value)
        {
            WriteHeader<PacketBindShaderVariableArray>(OpCode.BindShaderVariableFloatArray, sizeof(float) * value.Length, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Count = value.Length;

            var data = (float*)(packet + 1);
            for (var i = 0; i < value.Length; i++)
            {
                *data++ = value[i];
            }
        }

        public unsafe void BindShaderVariable(int uniformHandle, ref Vector3[] value)
        {
            WriteHeader<PacketBindShaderVariableArray>(OpCode.BindShaderVariableVector3Array, sizeof(Vector3) * value.Length, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Count = value.Length;

            var data = (Vector3*)(packet + 1);
            for (var i = 0; i < value.Length; i++)
            {
                *data++ = value[i];
            }
        }

        public unsafe void BindShaderVariable(int uniformHandle, ref Matrix4[] value)
        {
            WriteHeader<PacketBindShaderVariableArray>(OpCode.BindShaderVariableMatrix4Array, sizeof(Matrix4) * value.Length, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Count = value.Length;

            var data = (Matrix4*)(packet + 1);
            for (var i = 0; i < value.Length; i++)
            {
                *data++ = value[i];
            }
        }

        /// <summary>
        /// Bind an int value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, int value)
        {
            WriteHeader<PacketBindShaderVariableInt>(OpCode.BindShaderVariableInt, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->X = value;
        }

        /// <summary>
        /// Bind a float value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, float value)
        {
            WriteHeader<PacketBindShaderVariableFloat>(OpCode.BindShaderVariableFloat, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->X = value;
        }

        /// <summary>
        /// Bind a Vector4 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, ref Vector4 value)
        {
            WriteHeader<PacketBindShaderVariableVector4>(OpCode.BindShaderVariableVector4, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Value = value;
        }

        /// <summary>
        /// Bind a Vector3 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, ref Vector3 value)
        {
            WriteHeader<PacketBindShaderVariableVector3>(OpCode.BindShaderVariableVector3, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Value = value;
        }

        /// <summary>
        /// Bind a Vector2 value to the current shader
        /// </summary>
        /// <param name="uniformHandle"></param>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void BindShaderVariable(int uniformHandle, ref Vector2 value)
        {
            WriteHeader<PacketBindShaderVariableVector2>(OpCode.BindShaderVariableVector2, 0, out var packet);

            packet->UniformHandle = uniformHandle;
            packet->Value = value;
        }

        /// <summary>
        /// Draw a single mesh instance.
        /// BeginInstance has to be called before calling this and all shader variables should be bound
        /// </summary>
        /// <param name="handle"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawMesh(int handle)
        {
            WriteHeader<PacketDrawMesh>(OpCode.DrawMesh, 0, out var packet);

            packet->MeshHandle = handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawMesh(DrawMeshMultiData[] drawData, int count)
        {
            WriteHeader<PacketDrawMeshMulti>(OpCode.DrawMeshMulti, sizeof(DrawMeshMultiData) * count, out var packet);

            packet->Count = count;

            var data = (DrawMeshMultiData*)(packet + 1);
            for (var i = 0; i < count; i++)
            {
                *data++ = drawData[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DrawMeshOffset(int handle, int offset, int count)
        {
            WriteHeader<PacketDrawMeshOffset>(OpCode.DrawMeshOffset, 0, out var packet);

            packet->MeshHandle = handle;
            packet->Offset = offset;
            packet->Count = count;
        }

        /// <summary>
        /// Uploads a mesh inline in the command stream, UpdateMesh() is preferred if it not neccecary to change a mesh while rendering.
        /// </summary>
        public unsafe void UpdateMeshInline(int handle, int triangleCount, int vertexCount, int indexCount, float[] vertexData, int[] indexData, bool stream)
        {
            var dataSize = sizeof(float) * vertexCount + sizeof(int) * indexCount;
            WriteHeader<PacketUpdateMesh>(OpCode.UpdateMesh, dataSize, out var packet);

            var vd = (float*)(packet + 1);
            var id = (int*)(vd + vertexCount);

            packet->Handle = handle;
            packet->TriangleCount = triangleCount;
            packet->TriangleCount = triangleCount;
            packet->Stream = stream;
            packet->VertexCount = vertexCount;
            packet->IndexCount = indexCount;

            for (var i = 0; i < vertexCount; i++)
            {
                *vd++ = vertexData[i];
            }

            for (var i = 0; i < indexCount; i++)
            {
                *id++ = indexData[i];
            }
        }

        public unsafe void UpdateBufferInline(int handle, int size, byte* data)
        {
            WriteHeader<PacketUpdateBuffer>(OpCode.UpdateBuffer, size, out var packet);

            var d = (byte*)(packet + 1);

            packet->Handle = handle;
            packet->Size = size;

            for (var i = 0; i < size; i++)
            {
                *d++ = *data++;
            }
        }

        public unsafe void GenerateMips(int textureHandle)
        {
            WriteHeader<PacketGenerateMips>(OpCode.GenerateMips, 0, out var packet);

            packet->Handle = textureHandle;
        }

        public unsafe void ProfileBeginSection(HashedString name)
        {
            WriteHeader<PacketProfileSection>(OpCode.ProfileBegin, 0, out var packet);

            packet->Name = name;
        }

        public unsafe void ProfileEndSection(HashedString name)
        {
            WriteHeader<PacketProfileSection>(OpCode.ProfileEnd, 0, out var packet);

            packet->Name = name;
        }

        public unsafe void DispatchCompute(int numGroupsX, int numGroupsY, int numGroupsZ)
        {
            WriteHeader<PacketDispatchCompute>(OpCode.DispatchCompute, 0, out var packet);

            packet->NumGroupsX = numGroupsX;
            packet->NumGroupsY = numGroupsY;
            packet->NumGroupsZ = numGroupsZ;
        }

        public unsafe void Barrier(OpenTK.Graphics.OpenGL.MemoryBarrierFlags barrier)
        {
            WriteHeader<PacketBarrier>(OpCode.Barrier, 0, out var packet);

            packet->Barrier = barrier;
        }

        public unsafe void Scissor(bool enable, int x, int y, int w, int h)
        {
            WriteHeader<PacketScissor>(OpCode.Scissor, 0, out var packet);

            packet->Enable = enable;
            packet->X = x;
            packet->Y = y;
            packet->W = w;
            packet->H = h;
        }

        public unsafe void BindImageTexture(int unit, int texture, OGL.TextureAccess access, OGL.SizedInternalFormat format)
        {
            WriteHeader<PacketBindImageTexture>(OpCode.BindImageTexture, 0, out var packet);

            packet->Unit = unit;
            packet->TextureHandle = texture;
            packet->Access = access;
            packet->Format = format;
        }

        public unsafe void BindBufferBase(int index, int handle)
        {
            WriteHeader<PacketBindBufferBase>(OpCode.BindBufferBase, 0, out var packet);

            packet->Index = index;
            packet->Handle = handle;
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

        public Resources.Texture CreateTexture<T>(string name, int width, int height, PixelFormat pixelFormat, PixelInternalFormat interalFormat, PixelType pixelType, T[] data, bool mipmap)
            where T : struct
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
            EndPass, // NOP
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
            UpdateMesh,
            DrawMesh,
            DrawMeshMulti,
            DrawMeshOffset,
            DrawMeshInstanced,
            GenerateMips,
            ProfileBegin,
            ProfileEnd,
            DispatchCompute,
            UpdateBuffer,
            Scissor,
            BindImageTexture,
            BindBufferBase,
            Barrier,
            BeginPass2
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
        class CommandBuffer : IDisposable
        {
            private const int DefaultMemoryStreamSize = 1024 * 1024 * 64;

            public IntPtr Buffer;
            public int Length;

            public CommandBuffer()
            {
                Buffer = Marshal.AllocHGlobal(DefaultMemoryStreamSize);
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(Buffer);
                Buffer = IntPtr.Zero;
            }
        }
    }
}
