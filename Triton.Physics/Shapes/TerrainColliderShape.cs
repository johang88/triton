using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BulletSharp;

namespace Triton.Physics.Shapes
{
    public class TerrainColliderShape : IColliderShape
    {
        public Graphics.Terrain.TerrainData TerrainData { get; set; }
        private float[] _data;

        unsafe internal CollisionShape CreateCollisionShape()
        {
            // Convert heightmap to float & scale
            _data = new float[TerrainData.HeightMap.Length];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = (TerrainData.HeightMap[i] / (float)ushort.MaxValue) * TerrainData.MaxHeight;
            }

            fixed (float* p = _data)
            {
                var shape = new HeightfieldTerrainShape(TerrainData.Size, TerrainData.Size, (IntPtr)p, 1.0f, 0.0f, TerrainData.MaxHeight, 1, PhyScalarType.Single, false);
                shape.LocalScaling = new BulletSharp.Math.Vector3(TerrainData.MetersPerHeightfieldTexel, 1.0f, TerrainData.MetersPerHeightfieldTexel);

                return shape;
            }
        }
    }
}
