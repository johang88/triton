using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Terrain
{
	public class Terrain
	{
		public readonly TerrainData Data;
		public readonly Resources.Mesh Mesh;
		internal readonly Backend Backend;
		private List<Chunk> Chunks = new List<Chunk>();

		public Terrain(Backend backend, TerrainData data, int chunks, int resolution, Resources.Material material, Resources.Material compositeMaterial)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (data == null)
				throw new ArgumentNullException("data");

			Backend = backend;
			Data = data;

			var chunkSize = new Vector2(data.WorldSize.X, data.WorldSize.Z) / (float)chunks;

			// Setup the chunks
			for (var y = 0; y < chunks; y++)
			{
				for (var x = 0; x < chunks; x++)
				{
					var chunkPosition = new Vector2(chunkSize.X * x, chunkSize.Y * y);
					var chunk = new Chunk(this, chunkPosition, chunkSize, resolution, chunks);
					// TODO: Setup correct materials per chunk so that we can have different splatmaps etc ... 
					chunk.Mesh.Material = material;
					chunk.SplatMaterial = material;
					chunk.CompositeMaterial = compositeMaterial;
					Chunks.Add(chunk);
				}
			}

			Mesh = new Resources.Mesh("terrain", "");
			Mesh.SubMeshes = Chunks.Select(c => c.Mesh).ToArray();
		}

		public void Update(Vector3 cameraPosition)
		{
			foreach (var chunk in Chunks)
			{
				chunk.Update(cameraPosition);
			}
		}
	}
}
