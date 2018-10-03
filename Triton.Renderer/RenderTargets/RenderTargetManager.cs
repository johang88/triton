using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OGL = OpenTK.Graphics.OpenGL;

namespace Triton.Renderer.RenderTargets
{
    class RenderTargetManager : IDisposable
    {
        const int MaxHandles = 64;
        private readonly RenderTargetData[] _handles = new RenderTargetData[MaxHandles];
        private short _nextFree = 0;
        private bool _disposed = false;
        private readonly object _lock = new object();

        public RenderTargetManager()
        {
            // Each empty handle will store the location of the next empty handle 
            for (var i = 0; i < _handles.Length; i++)
            {
                _handles[i].Id = (short)(i + 1);
            }

            _handles[_handles.Length - 1].Id = -1;
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

            for (var i = 0; i < _handles.Length; i++)
            {
                if (_handles[i].Initialized)
                {
                    if (_handles[i].DepthBufferObject != 0 && !_handles[i].SharedDepth)
                        GL.DeleteRenderbuffers(1, ref _handles[i].DepthBufferObject);
                    GL.DeleteFramebuffers(1, ref _handles[i].FrameBufferObject);
                }
                _handles[i].Initialized = false;
            }

            _disposed = true;
        }

        int CreateHandle(int index, int id)
        {
            return (index << 16) | id;
        }

        void ExtractHandle(int handle, out int index, out int id)
        {
            id = handle & 0x0000FFFF;
            index = handle >> 16;
        }

        public int Create()
        {
            if (_nextFree == -1)
            {
                return CreateHandle(-1, -1);
            }

            int index;
            lock (_lock)
            {
                index = _nextFree;
                _nextFree = _handles[_nextFree].Id;
            }

            var id = ++_handles[index].Id;
            _handles[index].Initialized = false;

            return CreateHandle(index, id);
        }

        public void Init(int handle, Definition definition)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            GL.GenFramebuffers(1, out _handles[index].FrameBufferObject);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _handles[index].FrameBufferObject);

            var drawBuffers = new List<DrawBuffersEnum>();

            Common.Log.WriteLine("Creating frame buffer object, {0}x{1}", definition.Width, definition.Height);
            foreach (var attachment in definition.Attachments)
            {
                Common.Log.WriteLine(" - ap = {0}, pf = {1}, pif = {2}, pt = {3}", attachment.AttachmentPoint, attachment.PixelFormat, attachment.PixelInternalFormat, attachment.PixelType);

                if (attachment.AttachmentPoint == Definition.AttachmentPoint.Color)
                {
                    drawBuffers.Add(DrawBuffersEnum.ColorAttachment0 + attachment.Index);
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + attachment.Index, OGL.TextureTarget.Texture2D, attachment.TextureHandle, 0);
                }
                else if (attachment.AttachmentPoint == Definition.AttachmentPoint.Depth)
                {
                    if (attachment.Index == 0 && attachment.TextureHandle == 0)
                    {
                        GL.GenRenderbuffers(1, out _handles[index].DepthBufferObject);
                        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _handles[index].DepthBufferObject);
                        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, definition.Width, definition.Height);

                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _handles[index].DepthBufferObject);
                        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
                    }
                    else if (attachment.TextureHandle != 0)
                    {
                        GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, attachment.TextureHandle, 0);
                    }
                    else if (attachment.Index != 0)
                    {
                        var depthHandle = attachment.Index;
                        int depthIndex, depthId;
                        ExtractHandle(depthHandle, out depthIndex, out depthId);

                        _handles[index].SharedDepth = true;
                        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _handles[depthIndex].DepthBufferObject);
                    }
                }
            }

            if (drawBuffers.Count == 0)
            {
                drawBuffers.Add(DrawBuffersEnum.None);
                GL.DrawBuffer(DrawBufferMode.None);
            }

            _handles[index].DrawBuffers = drawBuffers.ToArray();

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Common.Log.WriteLine("Could not create framebuffer, status = " + status.ToString(), Common.LogLevel.Error);
                throw new Exception("Framebuffer not complete, " + status.ToString());
            }

            _handles[index].Definition = definition;
            _handles[index].Initialized = true;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Destroy(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id)
                return;

            lock (_lock)
            {
                if (_nextFree == -1)
                {
                    _handles[index].Id = -1;
                    _nextFree = (short)index;
                }
                else
                {
                    _handles[index].Id = _nextFree;
                    _nextFree = (short)index;
                }
            }

            if (_handles[index].Initialized)
            {
                if (_handles[index].DepthBufferObject != 0)
                    GL.DeleteRenderbuffers(1, ref _handles[index].DepthBufferObject);
                GL.DeleteFramebuffers(1, ref _handles[index].FrameBufferObject);
                _handles[index].DepthBufferObject = 0;
            }

            _handles[index].Initialized = false;
        }

        public Definition GetDefinition(int handle)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            return _handles[index].Definition;
        }

        public void Resize(int handle, int width, int height)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || _handles[index].DepthBufferObject == 0)
                return;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _handles[index].FrameBufferObject);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _handles[index].DepthBufferObject);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // The render target can now be used
            _handles[index].Initialized = true;
        }

        public int GetOpenGLHande(int handle, out DrawBuffersEnum[] drawBuffer)
        {
            int index, id;
            ExtractHandle(handle, out index, out id);

            if (id == -1 || _handles[index].Id != id || !_handles[index].Initialized)
            {
                drawBuffer = null;
                return 0;
            }

            drawBuffer = _handles[index].DrawBuffers;
            return _handles[index].FrameBufferObject;
        }

        struct RenderTargetData
        {
            public short Id;
            public bool Initialized;

            public int FrameBufferObject;
            public int DepthBufferObject;
            public bool SharedDepth;
            public DrawBuffersEnum[] DrawBuffers;

            public Definition Definition;
        }
    }
}
