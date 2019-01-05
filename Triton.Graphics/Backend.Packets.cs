using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Triton.Renderer;

namespace Triton.Graphics
{
    partial class Backend
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct PacketHeader
        {
            public OpCode OpCode;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBeginPass
        {
            public int Handle;
            public int Width;
            public int Height;
            public Vector4 ClearColor;
            public ClearFlags ClearFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBeginPass2
        {
            public int Handle;
            public int X;
            public int Y;
            public int W;
            public int H;
            public Vector4 ClearColor;
            public ClearFlags ClearFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBeginInstance
        {
            public int ShaderHandle;
            public int RenderStateId;
            public int NumberOfTextures;
            public int NumberOfSamplers;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableMatrix4
        {
            public int UniformHandle;
            public Matrix4 Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableUint2
        {
            public int UniformHandle;
            public uint X;
            public uint Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableInt
        {
            public int UniformHandle;
            public int X;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableFloat
        {
            public int UniformHandle;
            public float X;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableVector2
        {
            public int UniformHandle;
            public Vector2 Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableVector3
        {
            public int UniformHandle;
            public Vector3 Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableVector4
        {
            public int UniformHandle;
            public Vector4 Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindShaderVariableArray
        {
            public int UniformHandle;
            public int Count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketDrawMesh
        {
            public int MeshHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketDrawMeshMulti
        {
            public int Count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketDrawMeshOffset
        {
            public int MeshHandle;
            public int Offset;
            public int Count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketUpdateBuffer
        {
            public int Handle;
            public int Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketUpdateMesh
        {
            public int Handle;
            public int TriangleCount;
            public bool Stream;
            public int VertexCount;
            public int IndexCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketGenerateMips
        {
            public int Handle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketProfileSection
        {
            public int Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketDispatchCompute
        {
            public int NumGroupsX;
            public int NumGroupsY;
            public int NumGroupsZ;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBarrier
        {
            public OpenTK.Graphics.OpenGL.MemoryBarrierFlags Barrier;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketScissor
        {
            public bool Enable;
            public int X;
            public int Y;
            public int W;
            public int H;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindImageTexture
        {
            public int Unit;
            public int TextureHandle;
            public OpenTK.Graphics.OpenGL.TextureAccess Access;
            public OpenTK.Graphics.OpenGL.SizedInternalFormat Format;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PacketBindBufferBase
        {
            public int Index;
            public int Handle;
        }
    }
}
