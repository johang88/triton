using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics;
using Triton.Renderer;
using BulletSharp;
using BulletSharp.Math;
using Triton.Resources;

namespace Triton.Physics
{
    internal class DebugDrawer : BulletSharp.DebugDraw
    {
        private readonly Backend Backend;
        private readonly BatchBuffer Batch;
        private readonly Graphics.Resources.ShaderProgram Shader;
        private ShaderParams Params = null;
        public Vector3 Color = new Vector3(0, 1, 0);
        private int TriangleIndex = 0;
        private int RenderStateId;

        public bool ClockWise = false;

        public override DebugDrawModes DebugMode { get; set; }

        public DebugDrawer(Backend backend, ResourceManager resourceManager)
        {
            Backend = backend ?? throw new ArgumentNullException("backend");

            Batch = Backend.CreateBatchBuffer(new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
                {
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Color, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Normal, Renderer.VertexPointerType.Float, 3, sizeof(float) * 6),
                }));
            Batch.Begin();

            Shader = resourceManager.Load<Graphics.Resources.ShaderProgram>("/shaders/physic_debug_drawer");
            RenderStateId = backend.CreateRenderState(false, true, true, BlendingFactorSrc.One, BlendingFactorDest.One, wireFrame: true);

            DebugMode = DebugDrawModes.All;
        }

        public void Render(Camera camera)
        {
            if (Params == null)
            {
                Params = new ShaderParams();
                Shader.BindUniformLocations(Params);
            }

            camera.GetViewMatrix(out Matrix4 viewMatrix);

            camera.GetProjectionMatrix(out Matrix4 projectionMatrix);

            var modelViewProjectionMatrix = viewMatrix * projectionMatrix;

            Batch.End();

            Backend.BeginInstance(Shader.Handle, new int[0], new int[0], RenderStateId);

            Backend.BindShaderVariable(Params.HandleModelViewProjection, ref modelViewProjectionMatrix);
            Backend.BindShaderVariable(Params.HandleColor, ref Color);

            Backend.DrawMesh(Batch.MeshHandle);
            Backend.EndInstance();

            TriangleIndex = 0;
            Batch.Begin();
        }

        public override void DrawTriangle(ref BulletSharp.Math.Vector3 v0, ref BulletSharp.Math.Vector3 v1, ref BulletSharp.Math.Vector3 v2, ref BulletSharp.Math.Vector3 color, float alpha)
        {
            base.DrawTriangle(ref v0, ref v1, ref v2, ref color, alpha);

            var n = BulletSharp.Math.Vector3.Cross(v1 - v0, v2 - v0);

            Batch.AddVector3(v0.X, v0.Y, v0.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            Batch.AddVector3(v1.X, v1.Y, v1.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            Batch.AddVector3(v2.X, v2.Y, v2.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            if (!ClockWise)
                Batch.AddTriangle(TriangleIndex + 0, TriangleIndex + 1, TriangleIndex + 2);
            else
                Batch.AddTriangle(TriangleIndex + 0, TriangleIndex + 2, TriangleIndex + 1);

            TriangleIndex += 3;
        }

        public override void DrawLine(ref BulletSharp.Math.Vector3 from, ref BulletSharp.Math.Vector3 to, ref BulletSharp.Math.Vector3 color)
        {
            var pos1 = from;
            var pos2 = to;
            var pos3 = to;

            var n = BulletSharp.Math.Vector3.Cross(pos2 - pos1, pos3 - pos1);

            Batch.AddVector3(pos1.X, pos1.Y, pos1.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            Batch.AddVector3(pos2.X, pos2.Y, pos2.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            Batch.AddVector3(pos3.X, pos3.Y, pos3.Z);
            Batch.AddVector3(ref Color);
            Batch.AddVector3(n.X, n.Y, n.Z);

            if (!ClockWise)
                Batch.AddTriangle(TriangleIndex + 0, TriangleIndex + 1, TriangleIndex + 2);
            else
                Batch.AddTriangle(TriangleIndex + 0, TriangleIndex + 2, TriangleIndex + 1);

            TriangleIndex += 3;
        }

        public override void Draw3DText(ref BulletSharp.Math.Vector3 location, string textString)
        {
        }

        public override void ReportErrorWarning(string warningString)
        {
        }

        class ShaderParams
        {
            public int HandleModelViewProjection = 0;
            public int HandleColor = 0;
        }
    }
}
