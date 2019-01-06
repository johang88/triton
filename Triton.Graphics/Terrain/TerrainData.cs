using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;
using Triton.IO;

namespace Triton.Graphics.Terrain
{
    public class TerrainData
    {
        public int Size { get; set; }
        public ushort[] HeightMap { get; set; }
        public float MaxHeight { get; set; }
        public int NumberOfLodLevels { get; set; }
        public int MeshBaseLODExtentHeightfieldTexels { get; set; }
        public float MetersPerHeightfieldTexel { get; set; }

        public static TerrainData CreateFromFile(FileSystem fileSystem, string path)
        {
            using (var stream = fileSystem.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                var size = (int)(stream.Length / sizeof(ushort));
                var data = new ushort[size];

                for (var i = 0; i< size; i++)
                {
                    data[i] = reader.ReadUInt16();
                }

                size = (int)System.Math.Sqrt(size);

                return new TerrainData
                {
                    Size = size,
                    MaxHeight= 512.0f,
                    NumberOfLodLevels = 7,
                    MeshBaseLODExtentHeightfieldTexels = 128,
                    MetersPerHeightfieldTexel = 0.5f,
                    HeightMap = data
                };
            }
        }

        public float GetHeightAt(float x, float z)
        {
            x /= (Size * MetersPerHeightfieldTexel);
            z /= (Size * MetersPerHeightfieldTexel);

            return GetHeightAtTerrainPosition(x, z);
        }

        private float GetHeightAtTerrainPosition(float x, float z)
        {
            var factor = (float)Size - 1.0f;
            var invFactor = 1.0f / factor;

            var startX = (int)(x * factor);
            var startZ = (int)(z * factor);
            var endX = startX + 1;
            var endZ = startZ + 1;

            var startXTS = startX * invFactor;
            var startZTS = startZ * invFactor;
            var endXTS = endX * invFactor;
            var endZTS = endZ * invFactor;

            endX = System.Math.Min(endX, Size - 1);
            endZ = System.Math.Min(endZ, Size - 1);

            var xParam = (x - startXTS) / invFactor;
            var zParam = (z - startZTS) / invFactor;

            var v0 = new Vector3(startXTS, startZTS, GetHeightAtPoint(startX, startZ));
            var v1 = new Vector3(endXTS, startZTS, GetHeightAtPoint(endX, startZ));
            var v2 = new Vector3(endXTS, endZTS, GetHeightAtPoint(endX, endZ));
            var v3 = new Vector3(startXTS, endZTS, GetHeightAtPoint(startX, endZ));

            Plane plane;
            if (startZ % 2 > 0)
            {
                // odd row
                bool secondTri = ((1.0 - zParam) > xParam);
                if (secondTri)
                    plane = new Plane(v0, v1, v3);
                else
                    plane = new Plane(v1, v2, v3);
            }
            else
            {
                // even row
                bool secondTri = (zParam > xParam);
                if (secondTri)
                    plane = new Plane(v0, v2, v3);
                else
                    plane = new Plane(v0, v1, v2);
            }

            return (-plane.Normal.X * x
               - plane.Normal.Y * z
               - plane.D) / plane.Normal.Z;
        }

        private float GetHeightAtPoint(int x, int z)
        {
            int xi = (int)x, zi = (int)z;
            float xpct = x - xi, zpct = z - zi;

            if (xi == Size - 1)
            {
                --xi;
                xpct = 1.0f;
            }
            if (zi == Size - 1)
            {
                --zi;
                zpct = 1.0f;
            }

            var heights = new float[]
            {
                At(xi, zi),
                At(xi, zi + 1),
                At(xi + 1, zi),
                At(xi + 1, zi + 1)
            };

            var w = new float[]
            {
                (1.0f - xpct) * (1.0f - zpct),
                (1.0f - xpct) * zpct,
                xpct * (1.0f - zpct),
                xpct * zpct
            };

            var height = w[0] * heights[0] + w[1] * heights[1] + w[2] * heights[2] + w[3] * heights[3];
            height *= MaxHeight;

            return height;
        }

        private float At(int x, int z)
        {
            return HeightMap[z * Size + x] / (float)ushort.MaxValue;
        }

        public Vector3 GetNormalAt(float x, float z)
        {
            var flip = 1;
            var here = new Vector3(x, GetHeightAt(x, z), z);
            var left = new Vector3(x - 1.0f, GetHeightAt(x - 1.0f, z), z);
            var down = new Vector3(x, GetHeightAt(x, z + 1.0f), z + 1.0f);

            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1.0f, GetHeightAt(x + 1.0f, z), z);
            }

            if (down.Z >= (Size * MetersPerHeightfieldTexel) - 1)
            {
                flip *= -1;
                down = new Vector3(x, GetHeightAt(x, z - 1.0f), z - 1.0f);
            }

            left -= here;
            down -= here;

            var normal = Vector3.Cross(left, down) * flip;
            normal = normal.Normalize();

            return normal;
        }
    }
}
