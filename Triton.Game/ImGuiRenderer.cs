using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using Triton.Common;
using Triton.Graphics.Resources;
using Triton.Renderer;

namespace Triton.Game
{
    class ImGuiRenderer
    {
        private readonly Graphics.Backend _backend;
        private readonly int _meshHandle;
        private readonly int _vertexBufferHandle;
        private readonly int _indexBufferHandle;
        private readonly ShaderProgram _shader;
        private readonly int _renderState;
        private readonly int _sampler;
        private ShaderParams Params;

        public ImGuiRenderer(Graphics.Backend backend, ResourceManager resourceManager)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));

            _shader = resourceManager.Load<ShaderProgram>("/shaders/imgui");

            var renderSystem = backend.RenderSystem;

            var vertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
            {
                new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 2, DrawVert.PosOffset),
                new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, DrawVert.UVOffset),
                new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Color, Renderer.VertexPointerType.UnsignedByte, 4, DrawVert.ColOffset, normalized: true),
            });

            _vertexBufferHandle = renderSystem.CreateBuffer(Renderer.BufferTarget.ArrayBuffer, true, vertexFormat);
            _indexBufferHandle = renderSystem.CreateBuffer(Renderer.BufferTarget.ElementArrayBuffer, true);

            renderSystem.SetBufferData(_vertexBufferHandle, new byte[0], true, true);
            renderSystem.SetBufferData(_indexBufferHandle, new byte[0], true, true);

            _meshHandle = renderSystem.CreateMesh(0, _vertexBufferHandle, _indexBufferHandle, true, IndexType.UnsignedShort);

            _renderState = renderSystem.CreateRenderState(true, false, false, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, enableCullFace: false, scissorTest: true);

            _sampler = renderSystem.CreateSampler(new Dictionary<SamplerParameterName, int>
            {
                { SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear },
                { SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear }
            });

            CreateTextures();
        }

        unsafe void CreateTextures()
        {
            IO io = ImGui.GetIO();

            // Build texture atlas
            FontTextureData texData = io.FontAtlas.GetTexDataAsRGBA32();

            var length = texData.Width * texData.Height * texData.BytesPerPixel;
            byte[] data = new byte[length];
            Marshal.Copy((IntPtr)texData.Pixels, data, 0, length);

            var textureHandle = _backend.RenderSystem.CreateTexture(texData.Width, texData.Height, data, PixelFormat.Rgba, PixelInternalFormat.Rgba8, PixelType.UnsignedByte, false, null);

            // Store the texture identifier in the ImFontAtlas substructure.
            io.FontAtlas.SetTexID(textureHandle);

            // Cleanup (don't clear the input data if you want to append new fonts later)
            //io.Fonts->ClearInputData();
            io.FontAtlas.ClearTexData();
        }

        public unsafe void SubmitDrawCommands()
        {
            if (Params == null)
            {
                Params = new ShaderParams();
                _shader.BindUniformLocations(Params);
            }

            var io = ImGui.GetIO();
            var drawData = ImGui.GetDrawData();

            int width = (int)(io.DisplaySize.X * io.DisplayFramebufferScale.X);
            int height = (int)(io.DisplaySize.Y * io.DisplayFramebufferScale.Y);

            ImGui.ScaleClipRects(drawData, io.DisplayFramebufferScale);

            var projectionMatrix = Matrix4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

            for (int n = 0; n < drawData->CmdListsCount; n++)
            {
                NativeDrawList* commandList = drawData->CmdLists[n];
                byte* vertexBuffer = (byte*)commandList->VtxBuffer.Data;
                var indexBufferOffset = 0u;

                _backend.UpdateBufferInline(_vertexBufferHandle, commandList->VtxBuffer.Size * sizeof(DrawVert), (byte*)commandList->VtxBuffer.Data);
                _backend.UpdateBufferInline(_indexBufferHandle, commandList->IdxBuffer.Size * sizeof(ushort), (byte*)commandList->IdxBuffer.Data);

                for (int i = 0; i < commandList->CmdBuffer.Size; i++)
                {
                    var pcmd = &(((DrawCmd*)commandList->CmdBuffer.Data)[i]);

                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        _backend.BeginInstance(_shader.Handle, new int[] { pcmd->TextureId.ToInt32() }, new int[] { _sampler }, _renderState);
                        _backend.BindShaderVariable(Params.HandleDiffuseTexture, 0);
                        _backend.BindShaderVariable(Params.HandleModelViewProjection, ref projectionMatrix);
                        _backend.Scissor(true,
                            (int)pcmd->ClipRect.X,
                            (int)(io.DisplaySize.Y - pcmd->ClipRect.W),
                            (int)(pcmd->ClipRect.Z - pcmd->ClipRect.X),
                            (int)(pcmd->ClipRect.W - pcmd->ClipRect.Y));
                        _backend.DrawMeshOffset(_meshHandle, (int)indexBufferOffset, (int)pcmd->ElemCount);
                        _backend.EndInstance();
                    }

                    indexBufferOffset += pcmd->ElemCount * sizeof(ushort);
                }
                
                /* 
                Sample GL Implementation:
                const ImDrawList* cmd_list = draw_data->CmdLists[n];
                const ImDrawIdx* idx_buffer_offset = 0;

                glBindBuffer(GL_ARRAY_BUFFER, g_VboHandle);
                glBufferData(GL_ARRAY_BUFFER, (GLsizeiptr)cmd_list->VtxBuffer.Size * sizeof(ImDrawVert), (const GLvoid*)cmd_list->VtxBuffer.Data, GL_STREAM_DRAW);

                glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, g_ElementsHandle);
                glBufferData(GL_ELEMENT_ARRAY_BUFFER, (GLsizeiptr)cmd_list->IdxBuffer.Size * sizeof(ImDrawIdx), (const GLvoid*)cmd_list->IdxBuffer.Data, GL_STREAM_DRAW);

                for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
                {
                    const ImDrawCmd* pcmd = &cmd_list->CmdBuffer[cmd_i];
                    if (pcmd->UserCallback)
                    {
                        pcmd->UserCallback(cmd_list, pcmd);
                    }
                    else
                    {
                        glBindTexture(GL_TEXTURE_2D, (GLuint)(intptr_t)pcmd->TextureId);
                        glScissor((int)pcmd->ClipRect.x, (int)(fb_height - pcmd->ClipRect.w), (int)(pcmd->ClipRect.z - pcmd->ClipRect.x), (int)(pcmd->ClipRect.w - pcmd->ClipRect.y));
                        glDrawElements(GL_TRIANGLES, (GLsizei)pcmd->ElemCount, sizeof(ImDrawIdx) == 2 ? GL_UNSIGNED_SHORT : GL_UNSIGNED_INT, idx_buffer_offset);
                    }
                    idx_buffer_offset += pcmd->ElemCount;
                }
                */
            }
        }

        class ShaderParams
        {
            public int HandleModelViewProjection = 0;
            public int HandleDiffuseTexture = 0;
        }
    }
}
