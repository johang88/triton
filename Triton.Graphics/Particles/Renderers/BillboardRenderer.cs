using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Particles.Renderers
{
    public class BillboardRenderer : IParticleRenderer, IDisposable
    {
        [DataMember] public Resources.Material Material { get; set; }

        private Vector3 _commonDirection = -Vector3.UnitY;
        [DataMember] public Vector3 CommonDirection { get => _commonDirection; set => _commonDirection = value; }

        private Vector3 _commonUpVector = Vector3.UnitY;
        [DataMember] public Vector3 CommonUpVector { get => _commonUpVector; set => _commonUpVector = value; }

        [DataMember] public OrientationMode OrientationMode { get; set; }
        [DataMember] public Vector2 Size { get; set; } = Vector2.One;

        [DataMember] public bool AccurateFacing { get; set; } = false;

        private readonly BatchBuffer _buffer;

        public BillboardRenderer(Backend backend)
        {
            _buffer = new BatchBuffer(backend.RenderSystem, new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
                {
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 3),
                    new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Color, Renderer.VertexPointerType.Float, 4, sizeof(float) * 5),
                }));
        }

        public void Dispose()
            => _buffer.Dispose();

        public void PrepareRenderOperations(ParticleSystem particleSystem, RenderOperations operations, Matrix4 worldOffset)
        {
            operations.Add(_buffer.MeshHandle, Matrix4.Identity, Material);
        }

        public void Update(ParticleSystem particleSystem, Stage stage, float deltaTime)
        {
            _buffer.Begin();

            var points = new Vector3[4];
            var positions = new Vector3[4];

            var camera = stage.Camera;

            var cameraOrientation = stage.Camera.Orientation;
            if (!particleSystem.WorldSpace)
            {
                Quaternion.Invert(ref particleSystem.Orientation, out var invOrientation);

                Quaternion.Multiply(ref invOrientation, ref camera.Orientation, out cameraOrientation);

                Vector3.Subtract(ref particleSystem.Position, ref camera.Position, out var cameraPosition);
                Vector3.Transform(ref cameraPosition, ref invOrientation, out cameraPosition);
            }

            camera.GetUpVector(out var upVector);
            
            Vector3 camDir = Vector3.Zero, x = Vector3.Zero, y = Vector3.Zero, unitX = Vector3.UnitX, unitY = Vector3.UnitY;

            if (!AccurateFacing)
            {
                camDir = Vector3.Transform(-Vector3.UnitZ, cameraOrientation);
            }

            _commonDirection.Normalize();
            _commonUpVector.Normalize();

            switch (OrientationMode)
            {
                case OrientationMode.Point:
                    if (!AccurateFacing)
                    {
                        Vector3.Transform(ref unitX, ref cameraOrientation, out x);
                        Vector3.Transform(ref unitY, ref cameraOrientation, out y);
                    }
                    break;
                case OrientationMode.OrientedCommon:
                    if (!AccurateFacing)
                    {
                        y = _commonDirection;
                        Vector3.Cross(ref camDir, ref y, out x);
                    }
                    break;
                case OrientationMode.PerpendicularCommon:
                    Vector3.Cross(ref _commonUpVector, ref _commonDirection, out x);
                    Vector3.Cross(ref _commonDirection, ref x, out y);
                    break;
            }

            var halfWidth = Size.X * 0.5f;
            var halfHeight = Size.Y * 0.5f;

            var index = 0;

            var particles = particleSystem.Particles;
            for (var i = 0; i < particles.AliveCount; i++)
            {
                switch (OrientationMode)
                {
                    case OrientationMode.Point:
                        if (AccurateFacing)
                        {
                            Vector3.Cross(ref upVector, ref camDir, out x);
                            x.Normalize();

                            Vector3.Cross(ref camDir, ref x, out y);
                        }
                        break;
                    case OrientationMode.OrientedCommon:
                        if (AccurateFacing)
                        {
                            y = CommonDirection;
                            Vector3.Cross(ref camDir, ref y, out x);
                        }
                        break;
                    case OrientationMode.OrientedSelf:
                        Vector3.Cross(ref camDir, ref particles.Velocity[i], out x);
                        break;
                    case OrientationMode.PerpendicularSelf:
                        Vector3.Cross(ref _commonUpVector, ref particles.Velocity[i], out x);
                        Vector3.Cross(ref particles.Velocity[i], ref x, out y);
                        break;
                }

                var scale = 1.0f; // TODO

                Vector3.Multiply(ref x, (-halfWidth * scale), out var vLeftOff);
                Vector3.Multiply(ref x, (halfWidth * scale), out var vRightOff);
                Vector3.Multiply(ref y, (halfHeight * scale), out var vTopOff);
                Vector3.Multiply(ref y, (-halfHeight * scale), out var vBottomOff);

                Vector3.Add(ref vLeftOff, ref vTopOff, out points[0]);
                Vector3.Add(ref vRightOff, ref vTopOff, out points[1]);
                Vector3.Add(ref vLeftOff, ref vBottomOff, out points[2]);
                Vector3.Add(ref vRightOff, ref vBottomOff, out points[3]);

                // TODO: Handle rotation
                Vector3.Add(ref points[0], ref particles.Position[i], out positions[0]);
                Vector3.Add(ref points[1], ref particles.Position[i], out positions[1]);
                Vector3.Add(ref points[2], ref particles.Position[i], out positions[2]);
                Vector3.Add(ref points[3], ref particles.Position[i], out positions[3]);

                // Add to mesh buffer
                var color = particles.Color[i];

                _buffer.AddVector3(ref positions[0]);
                _buffer.AddVector2(0, 0);
                _buffer.AddVector4(ref color);

                _buffer.AddVector3(ref positions[1]);
                _buffer.AddVector2(1, 0);
                _buffer.AddVector4(ref color);

                _buffer.AddVector3(ref positions[2]);
                _buffer.AddVector2(0, 1);
                _buffer.AddVector4(ref color);

                _buffer.AddVector3(ref positions[3]);
                _buffer.AddVector2(1, 1);
                _buffer.AddVector4(ref color);

                _buffer.AddTriangle(index + 0, index + 1, index + 2);
                _buffer.AddTriangle(index + 1, index + 3, index + 2);
                index += 4;
            }

            _buffer.End();
        }
    }
}
