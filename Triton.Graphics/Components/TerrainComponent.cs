using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;
using Triton.Renderer;
using Triton.Terrain;

namespace Triton.Graphics.Components
{
    public class TerrainComponent : RenderableComponent
    {
        public TerrainData TerrainData { get; set; }
        public Material Material { get; set; }

        private BatchBuffer _batchBuffer;
        private Texture _heightMap;
        private Texture _normalMap;

        public override void OnActivate()
        {
            base.OnActivate();

            // Create geo mesh
            var backend = Services.Get<Backend>();
            var vertexFormat = new VertexFormat(new VertexFormatElement[]
            {
                new VertexFormatElement(VertexFormatSemantic.Position, VertexPointerType.Float, 3, 0)
            });

            _batchBuffer = new BatchBuffer(backend.RenderSystem, vertexFormat);
            BuildMesh();
            _batchBuffer.End();

            // Convert heightmap to float
            var data = new float[TerrainData.HeightMap.Length];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = TerrainData.HeightMap[i] / (float)ushort.MaxValue;
            }

            // Calculate normal map
            var normalMapData = new Vector3[TerrainData.Size * TerrainData.Size];
            for (var z = 0; z < TerrainData.Size; z++)
            {
                for (var x = 0; x < TerrainData.Size; x++)
                {
                    var position = new Vector2(x * TerrainData.MetersPerHeightfieldTexel, z * TerrainData.MetersPerHeightfieldTexel);
                    normalMapData[z * TerrainData.Size + x] = TerrainData.GetNormalAt(position.X, position.Y) * 0.5f + new Vector3(0.5f, 0.5f, 0.5f);
                }
            }

            // Create GPU heightmap & normalmap textures
            _heightMap = backend.CreateTexture("/system/terrain_heightmap", TerrainData.Size, TerrainData.Size, PixelFormat.Red, PixelInternalFormat.R16f, PixelType.Float, data, true);
            _normalMap = backend.CreateTexture("/system/terrain_normalmap", TerrainData.Size, TerrainData.Size, PixelFormat.Rgb, PixelInternalFormat.Rgb8, PixelType.Float, normalMapData, true);

            Material.Textures["samplerHeightMap"] = _heightMap;
            Material.Textures["samplerNormalMap"] = _normalMap;
            Material.SetUniform("uTerrainParameters", new Vector4(
                TerrainData.MetersPerHeightfieldTexel,
                1.0f / TerrainData.MetersPerHeightfieldTexel,
                1.0f / (float)TerrainData.MeshBaseLODExtentHeightfieldTexels,
                TerrainData.MaxHeight
                ));

            Material.SetUniform("uTerrainParameters2", new Vector4(
               1.0f / (float)_heightMap.Width,
               1.0f / (float)_heightMap.Height,
               0,
               0
               ));

            // It's really big!
            BoundingSphere.Radius = 32000;
        }

        private void BuildMesh()
        {
            var baseIndex = 0;
            for (int level = 0; level < TerrainData.NumberOfLodLevels; level++)
            {
                var step = (1 << level);
                var prevStep = System.Math.Max(0, (1 << (level - 1)));
                var halfStep = prevStep;

                var g = TerrainData.MeshBaseLODExtentHeightfieldTexels / 2;
                var L = (float)level;

                var pad = 1;
                var radius = step * (g + pad);
                for (var z = -radius; z < radius; z += step)
                {
                    for (var x = -radius; x < radius; x += step)
                    {
                        if (System.Math.Max(System.Math.Abs(x + halfStep), System.Math.Abs(z + halfStep)) >= g * prevStep)
                        {
                            var A = new Vector3((float)x, L, (float)z);
                            var C = new Vector3((float)x + step, L, A.Z);
                            var G = new Vector3(A.X, L, (float)z + step);
                            var I = new Vector3(C.X, L, G.Z);

                            var B = (A + C) * 0.5f;
                            var D = (A + G) * 0.5f;
                            var F = (C + I) * 0.5f;
                            var H = (G + I) * 0.5f;

                            var E = (A + I) * 0.5f;

                            A.Y = B.Y = C.Y = D.Y = E.Y = F.Y = G.Y = H.Y = I.Y = L;

                            _batchBuffer.AddVector3(ref A);
                            _batchBuffer.AddVector3(ref B);
                            _batchBuffer.AddVector3(ref C);
                            _batchBuffer.AddVector3(ref D);
                            _batchBuffer.AddVector3(ref E);
                            _batchBuffer.AddVector3(ref F);
                            _batchBuffer.AddVector3(ref G);
                            _batchBuffer.AddVector3(ref H);
                            _batchBuffer.AddVector3(ref I);

                            var AI = baseIndex + 0;
                            var BI = baseIndex + 1;
                            var CI = baseIndex + 2;
                            var DI = baseIndex + 3;
                            var EI = baseIndex + 4;
                            var FI = baseIndex + 5;
                            var GI = baseIndex + 6;
                            var HI = baseIndex + 7;
                            var II = baseIndex + 8;

                            baseIndex += 9;

                            if (x == -radius)
                            {
                                _batchBuffer.AddTriangle(EI, AI, GI);
                            }
                            else
                            {
                                _batchBuffer.AddTriangle(EI, AI, DI);
                                _batchBuffer.AddTriangle(EI, DI, GI);
                            }

                            if (z == radius - 1)
                            {
                                _batchBuffer.AddTriangle(EI, GI, II);
                            }
                            else
                            {
                                _batchBuffer.AddTriangle(EI, GI, HI);
                                _batchBuffer.AddTriangle(EI, HI, II);
                            }

                            if (x == radius - 1)
                            {
                                _batchBuffer.AddTriangle(EI, II, CI);
                            }
                            else
                            {
                                _batchBuffer.AddTriangle(EI, II, FI);
                                _batchBuffer.AddTriangle(EI, FI, CI);
                            }

                            if (z == -radius)
                            {
                                _batchBuffer.AddTriangle(EI, CI, AI);
                            }
                            else
                            {
                                _batchBuffer.AddTriangle(EI, CI, BI);
                                _batchBuffer.AddTriangle(EI, BI, AI);
                            }
                        }
                    }
                }
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            _batchBuffer?.Dispose();
            _batchBuffer = null;

            _heightMap?.Dispose();
            _heightMap = null;

            _normalMap?.Dispose();
            _normalMap = null;
        }

        public override void PrepareRenderOperations(BoundingFrustum frustum, RenderOperations operations)
        {
            operations.Add(_batchBuffer.MeshHandle, Matrix4.Identity, Material, castShadows: false);
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            // Terrain never cast shadows, will need custom solution
            CastShadows = false;
        }
    }
}
