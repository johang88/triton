using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OGL = OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using Triton.Utility;
using Triton.Logging;
using System.Runtime.CompilerServices;
using Triton.Renderer.Meshes;

namespace Triton.Renderer
{
    public class ContextReference
    {
        public OpenTK.ContextHandle Handle;
        public GraphicsContext.GetAddressDelegate GetAddress;
        public GraphicsContext.GetCurrentContextDelegate GetCurrent;
        public Action SwapBuffers;
        public OpenTK.Graphics.IGraphicsContext Context; // We can also provide the opentk context instance directly if we so desire
    }

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
        private readonly IGraphicsContext _context;
        private readonly bool _ownsContext = false;

        private readonly Textures.TextureManager _textureManager;
        private readonly Meshes.MeshManager _meshManager;
        private readonly Meshes.BufferManager _bufferManager;
        private readonly Shaders.ShaderManager _shaderManager;
        private readonly RenderTargets.RenderTargetManager _renderTargetManager;
        private readonly RenderStates.RenderStateManager _renderStateManager;
        private readonly Samplers.SamplerManager _samplerManager;

        private DebugProc _debugProcCallback;

        private bool _disposed = false;
        private readonly Action<Action> _addToWorkQueue;

        public delegate void OnLoadedCallback(int handle, bool success, string errors);

        public RenderSystem(Action<Action> addToWorkQueue, ContextReference ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            _addToWorkQueue = addToWorkQueue ?? throw new ArgumentNullException(nameof(addToWorkQueue));

            var graphicsMode = new GraphicsMode(32, 24, 0, 0);

            if (ctx.Context != null)
            {
                _ownsContext = false;
                _context = ctx.Context;
            }
            else
            {
                // We dont actually own this context but I am pretty sure that we should dispose the calls so we let _ownsContext be false
                _context = new GraphicsContext(ctx.Handle, ctx.GetAddress, ctx.GetCurrent);
                _context.LoadAll();
            }

            var major = GL.GetInteger(GetPName.MajorVersion);
            var minor = GL.GetInteger(GetPName.MinorVersion);

            Log.WriteLine("OpenGL Context ({0}.{1}) initialized", major, minor);
            Log.WriteLine(" - Color format: {0}", _context.GraphicsMode.ColorFormat);
            Log.WriteLine(" - Depth: {0}", _context.GraphicsMode.Depth);
            Log.WriteLine(" - FSAA Samples: {0}", _context.GraphicsMode.Samples);

            GLWrapper.Initialize();

            _textureManager = new Textures.TextureManager();
            _bufferManager = new Meshes.BufferManager();
            _meshManager = new Meshes.MeshManager(_bufferManager);
            _shaderManager = new Shaders.ShaderManager();
            _renderTargetManager = new RenderTargets.RenderTargetManager();
            _renderStateManager = new RenderStates.RenderStateManager();
            _samplerManager = new Samplers.SamplerManager();

            var initialRenderState = _renderStateManager.CreateRenderState(false, true, true, BlendingFactorSrc.Zero, BlendingFactorDest.One, CullFaceMode.Back, true, DepthFunction.Less);
            _renderStateManager.ApplyRenderState(initialRenderState);

            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.Enable(EnableCap.TextureCubeMapSeamless);

            _debugProcCallback = DebugCallback;
            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);

            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing || _disposed)
                return;

            _samplerManager.Dispose();
            _textureManager.Dispose();
            _meshManager.Dispose();
            _bufferManager.Dispose();
            _shaderManager.Dispose();
            _renderTargetManager.Dispose();

            if (_ownsContext)
                _context.Dispose();

            _disposed = true;
        }

        private void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (severity == DebugSeverity.DebugSeverityLow || severity == DebugSeverity.DebugSeverityMedium || severity == DebugSeverity.DebugSeverityHigh)
            {
                var msg = Marshal.PtrToStringAnsi(message, length);
                Log.WriteLine(string.Format("{0} {1} {2}", severity, type, msg));
            }
        }

        public int CreateTexture(int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type, bool mipmap, OnLoadedCallback loadedCallback)
        {
            var handle = _textureManager.Create();
            SetTextureData(handle, width, height, data, format, internalFormat, type, mipmap, loadedCallback);

            return handle;
        }

        public void DestroyTexture(int handle)
        {
            _addToWorkQueue(() => _textureManager.Destroy(handle));
        }

        public int CreateFromDDS(byte[] data, out int width, out int height)
        {
            var handle = _textureManager.Create();

            _textureManager.LoadDDS(handle, data, out width, out height);

            return handle;
        }

        public void SetTextureData(int handle, int width, int height, byte[] data, PixelFormat format, PixelInternalFormat internalFormat, PixelType type, bool mipmap, OnLoadedCallback loadedCallback)
        {
            Action loadAction = () =>
            {
                _textureManager.SetPixelData(handle, TextureTarget.Texture2D, width, height, data, format, internalFormat, type, mipmap);

                loadedCallback?.Invoke(handle, true, "");
            };

            _addToWorkQueue(loadAction);
        }

        public void BindTexture(int handle, int textureUnit)
        {
            _textureManager.Bind(textureUnit, handle);
        }

        public void GenreateMips(int handle)
        {
            OGL.TextureTarget target;
            var openGLHandle = _textureManager.GetOpenGLHande(handle, out target);

            if (GLWrapper.ExtDirectStateAccess)
            {
                GL.Ext.GenerateTextureMipmap(openGLHandle, target);
            }
            else
            {
                var current = _textureManager.GetActiveTexture(0);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(target, openGLHandle);
                GL.GenerateMipmap((OGL.GenerateMipmapTarget)(int)target);

                GL.BindTexture(target, current);
            }
        }

        public int CreateBuffer(BufferTarget target, bool mutable, VertexFormat vertexFormat = null)
        {
            return _bufferManager.Create(target, mutable, vertexFormat);
        }

        public void DestroyBuffer(int handle)
        {
            _addToWorkQueue(() => _bufferManager.Destroy(handle));
        }

        public void SetBufferData<T>(int handle, T[] data, bool stream, bool async)
            where T : struct
        {
            if (async)
            {
                _addToWorkQueue(() =>
                {
                    _bufferManager.SetData(handle, data, stream);
                });
            }
            else
            {
                _bufferManager.SetData(handle, data, stream);
            }
        }

        public void SetBufferDataDirect(int handle, IntPtr length, IntPtr data, bool stream)
        {
            _bufferManager.SetDataDirect(handle, length, data, stream);
        }

        public int CreateMesh(int triangleCount, int vertexBuffer, int indexBuffer, bool async, IndexType indexType = IndexType.UnsignedInt)
        {
            var handle = _meshManager.Create();

            if (async)
            {
                _addToWorkQueue(() =>
                {
                    _meshManager.Initialize(handle, triangleCount, vertexBuffer, indexBuffer, indexType);
                });
            }
            else
            {
                _meshManager.Initialize(handle, triangleCount, vertexBuffer, indexBuffer, indexType);
            }

            return handle;
        }

        public void DestroyMesh(int handle)
        {
            _addToWorkQueue(() =>
            {
                _meshManager.Destroy(handle);
            });
        }

        public void SetMeshDataDirect(int handle, int triangleCount, IntPtr vertexDataLength, IntPtr indexDataLength, IntPtr vertexData, IntPtr indexData, bool stream)
        {
            int vertexBufferId, indexBufferId;
            _meshManager.GetMeshData(handle, out vertexBufferId, out indexBufferId);

            _bufferManager.SetDataDirect(vertexBufferId, vertexDataLength, vertexData, stream);
            _bufferManager.SetDataDirect(indexBufferId, indexDataLength, indexData, stream);

            _meshManager.SetTriangleCount(handle, triangleCount);
        }

        public void MeshSetTriangleCount(int handle, int triangleCount, bool queue)
        {
            if (queue)
            {
                _addToWorkQueue(() => _meshManager.SetTriangleCount(handle, triangleCount));
            }
            else
            {
                _meshManager.SetTriangleCount(handle, triangleCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RenderMesh(int handle, PrimitiveType primitiveType)
        {
            _meshManager.Render(handle, primitiveType);
        }

        public unsafe void RenderMesh(DrawMeshMultiData* meshIndices, int count)
        {
            _meshManager.Render(meshIndices, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RenderMesh(int handle, PrimitiveType primitiveType, int offset, int count)
        {
            _meshManager.Render(handle, primitiveType, offset, count);
        }

        public void BeginScene(int renderTargetHandle, int width, int height)
        {
            _renderStateManager.ApplyRenderState(0);
            BindRenderTarget(renderTargetHandle);
            GL.Viewport(0, 0, width, height);
        }

        public void BeginScene(int renderTargetHandle, int x, int y, int width, int height)
        {
            _renderStateManager.ApplyRenderState(0);
            BindRenderTarget(renderTargetHandle);
            GL.Viewport(x, y, width, height);
        }

        public void SwapBuffers()
        {
            _context.SwapBuffers();
        }

        public int CreateShader()
        {
            return _shaderManager.Create();
        }

        public void DestroyShader(int handle)
        {
            _addToWorkQueue(() => _shaderManager.Destroy(handle));
        }

        public bool SetShaderData(int handle, Dictionary<ShaderType, string> sources, out string errors)
        {
            return _shaderManager.SetShaderData(handle, sources, out errors);
        }

        public void BindShader(int handle)
        {
            _shaderManager.Bind(handle);
        }

        public void DispatchCompute(int numGroupsX, int numGroupsY, int numGroupsZ)
        {
            GL.DispatchCompute(numGroupsX, numGroupsY, numGroupsZ);
        }

        public int GetUniformLocation(int handle, string name)
        {
            var programHandle = _shaderManager.GetOpenGLHande(handle);
            return GL.GetUniformLocation(programHandle, name);
        }

        public Dictionary<HashedString, int> GetUniforms(int handle)
        {
            return _shaderManager.GetUniforms(handle);
        }

        public void SetUniformFloat(int handle, float value)
        {
            GL.Uniform1(handle, value);
        }

        public void SetUniformInt(int handle, int value)
        {
            GL.Uniform1(handle, value);
        }

        public unsafe void SetUniformVector2(int handle, int count, float* value)
        {
            GL.Uniform2(handle, count, value);
        }

        public unsafe void SetUniformVector2u(int handle, int count, uint* value)
        {
            GL.Uniform2(handle, count, value);
        }

        public unsafe void SetUniformMatrix4(int handle, int count, float* value)
        {
            GL.UniformMatrix4(handle, count, false, value);
        }

        public unsafe void SetUniformVector3(int handle, int count, float* value)
        {
            GL.Uniform3(handle, count, value);
        }

        public unsafe void SetUniformVector4(int handle, int count, float* value)
        {
            GL.Uniform4(handle, count, value);
        }

        public unsafe void SetUniformInt(int handle, int count, int* value)
        {
            GL.Uniform1(handle, count, value);
        }

        public unsafe void SetUniformFloat(int handle, int count, float* value)
        {
            GL.Uniform1(handle, count, value);
        }

        public void BindImageTexture(int unit, int texture, TextureAccess access, SizedInternalFormat format)
        {
            var glHandle = _textureManager.GetOpenGLHande(texture);
            GL.BindImageTexture(unit, glHandle, 0, false, 0, access, format);
        }

        public void BindBufferBase(int index, int handle)
        {
            _bufferManager.GetOpenGLHandle(handle, out var buffer, out var target);
            GL.BindBufferBase((BufferRangeTarget)(int)target, index, buffer);
        }

        public void BindBufferRange(int index, int handle, IntPtr offset, IntPtr size)
        {
            _bufferManager.GetOpenGLHandle(handle, out var buffer, out var target);
            GL.BindBufferRange((BufferRangeTarget)(int)target, index, buffer, offset, size);
        }

        public void Clear(Triton.Vector4 clearColor, ClearFlags flags)
        {
            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W);

            ClearBufferMask mask = 0;
            if ((flags & ClearFlags.Color) == ClearFlags.Color)
            {
                mask |= ClearBufferMask.ColorBufferBit;
            }

            if ((flags & ClearFlags.Depth) == ClearFlags.Depth)
            {
                mask |= ClearBufferMask.DepthBufferBit;
            }

            GL.Clear(mask);
        }

        public int CreateRenderState(bool enableAlphaBlend = false, bool enableDepthWrite = true, bool enableDepthTest = true, BlendingFactorSrc src = BlendingFactorSrc.Zero, BlendingFactorDest dest = BlendingFactorDest.One, CullFaceMode cullFaceMode = CullFaceMode.Back, bool enableCullFace = true, DepthFunction depthFunction = DepthFunction.Less, bool wireFrame = false, bool scissorTest = false)
        {
            return _renderStateManager.CreateRenderState(enableAlphaBlend, enableDepthWrite, enableDepthTest, src, dest, cullFaceMode, enableCullFace, depthFunction, wireFrame, scissorTest);
        }

        public void SetRenderState(int id)
        {
            _renderStateManager.ApplyRenderState(id);
        }

        public int CreateRenderTarget(RenderTargets.Definition definition, out int[] textureHandles, OnLoadedCallback loadedCallback)
        {
            var internalTextureHandles = new List<int>();

            foreach (var attachment in definition.Attachments)
            {
                if (attachment.AttachmentPoint == RenderTargets.Definition.AttachmentPoint.Color || definition.RenderDepthToTexture)
                {
                    var textureHandle = _textureManager.Create();
                    internalTextureHandles.Add(textureHandle);
                    attachment.TextureHandle = textureHandle;
                }
            }

            textureHandles = internalTextureHandles.ToArray();

            var renderTargetHandle = _renderTargetManager.Create();

            _addToWorkQueue(() =>
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

                        _textureManager.SetPixelData(attachment.TextureHandle, target, definition.Width, definition.Height, null, attachment.PixelFormat, attachment.PixelInternalFormat, attachment.PixelType, attachment.MipMaps);
                        // Replace with gl handle
                        attachment.TextureHandle = _textureManager.GetOpenGLHande(attachment.TextureHandle);
                    }
                }

                // Init render target
                _renderTargetManager.Init(renderTargetHandle, definition);

                loadedCallback?.Invoke(renderTargetHandle, true, "");
            });

            return renderTargetHandle;
        }
        
        public void ResizeRenderTarget(int handle, int width, int height)
        {
            _addToWorkQueue(() =>
            {
                _renderTargetManager.Resize(handle, width, height);

                // Resize textures
                var definition = _renderTargetManager.GetDefinition(handle);

                foreach (var attachment in definition.Attachments)
                {
                    if (attachment.AttachmentPoint == RenderTargets.Definition.AttachmentPoint.Color || definition.RenderDepthToTexture)
                    {
                        var target = TextureTarget.Texture2D;

                        if (definition.RenderToCubeMap)
                        {
                            target = TextureTarget.TextureCubeMap;
                        }

                        GL.BindTexture((OGL.TextureTarget)(int)target, attachment.TextureHandle);

                        if (target == TextureTarget.TextureCubeMap)
                        {
                            for (var i = 0; i < 6; i++)
                            {
                                GL.TexImage2D(OGL.TextureTarget.TextureCubeMapPositiveX + i, 0, (OGL.PixelInternalFormat)(int)attachment.PixelInternalFormat, width, height, 0, (OGL.PixelFormat)(int)attachment.PixelFormat, (OGL.PixelType)(int)attachment.PixelFormat, IntPtr.Zero);
                            }
                        }
                        else
                        {
                            GL.TexImage2D((OGL.TextureTarget)(int)target, 0, (OGL.PixelInternalFormat)(int)attachment.PixelInternalFormat, width, height, 0, (OGL.PixelFormat)(int)attachment.PixelFormat, (OGL.PixelType)(int)attachment.PixelType, IntPtr.Zero);
                        }
                    }
                }
            });
        }

        public void DestroyRenderTarget(int handle)
        {
            _addToWorkQueue(() => _renderTargetManager.Destroy(handle));
        }

        public void BindRenderTarget(int handle)
        {
            DrawBuffersEnum[] drawBuffers;
            var openGLHandle = _renderTargetManager.GetOpenGLHande(handle, out drawBuffers);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, openGLHandle);

            if (drawBuffers == null)
            {
                GL.DrawBuffer(DrawBufferMode.Back);
                GL.ColorMask(true, true, true, true);
            }
            else if (drawBuffers.Length == 1 && drawBuffers[0] == DrawBuffersEnum.None)
            {
                GL.ColorMask(false, false, false, false);
            }
            else
            {
                GL.DrawBuffers(drawBuffers.Length, drawBuffers);
                GL.ColorMask(true, true, true, true);
            }
        }

        public int CreateSampler(Dictionary<SamplerParameterName, int> settings)
        {
            var handle = _samplerManager.Create();

            _addToWorkQueue(() =>
            {
                _samplerManager.Init(handle, settings);
            });

            return handle;
        }

        public void BindSampler(int textureUnit, int handle)
        {
            _samplerManager.Bind(textureUnit, handle);
        }

        public void SetWireFrameEnabled(bool enabled)
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, enabled ? PolygonMode.Line : PolygonMode.Fill);
        }

        public void Scissor(bool enable, int x, int y, int w, int h)
        {
            if (enable)
            {
                GL.Enable(EnableCap.ScissorTest);
                GL.Scissor(x, y, w, h);
            }
            else
            {
                GL.Disable(EnableCap.ScissorTest);
            }
        }

        public void ReadTexture<T>(int handle, PixelFormat format, PixelType type, ref T pixels) where T : struct
        {
            OGL.TextureTarget target;
            var glHandle = _textureManager.GetOpenGLHande(handle, out target);
            GL.Ext.GetTextureImage<T>(glHandle, target, 0, (OGL.PixelFormat)format, (OGL.PixelType)type, ref pixels);
        }

        public void ReadTexture<T>(int handle, PixelFormat format, PixelType type, T[] pixels) where T : struct
        {
            OGL.TextureTarget target;
            var glHandle = _textureManager.GetOpenGLHande(handle, out target);
            GL.Ext.GetTextureImage<T>(glHandle, target, 0, (OGL.PixelFormat)format, (OGL.PixelType)type, pixels);
        }
    }
}
