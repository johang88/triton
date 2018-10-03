using Jitter.Collision;
using Jitter.LinearMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Resources
{
	class MeshLoader : Triton.Common.IResourceLoader<Mesh>
	{
		static readonly char[] Magic = new char[] { 'C', 'O', 'L', 'M' };
		const int Version_1_0 = 0x0100;

		private readonly Triton.Common.IO.FileSystem FileSystem;
		private readonly Triton.Common.ResourceManager ResourceManager;

		public MeshLoader(Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			FileSystem = fileSystem;
			ResourceManager = resourceManager;
		}

		public string Extension { get { return ".col"; } }
		public string DefaultFilename { get { return ""; } }

		public object Create(string name, string parameters)
		{
			return new Mesh(name, parameters);
		}

		public void Load(object resource, byte[] data)
		{
			Unload(resource);

			var mesh = (Mesh)resource;

			using (var stream = new System.IO.MemoryStream(data))
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var magic = reader.ReadChars(4);
				for (var i = 0; i < Magic.Length; i++)
				{
					if (magic[i] != Magic[i])
						throw new ArgumentException("invalid mesh");
				}

				var version = reader.ReadInt32();

				var validVersions = new int[] { Version_1_0 };

				if (!validVersions.Contains(version))
					throw new ArgumentException("invalid mesh, unknown version");

				var isConvexHull = reader.ReadBoolean();
				var vertexCount = reader.ReadInt32();
				var indexCount = reader.ReadInt32();

				var vertices = new List<JVector>();
				var indices = new List<TriangleVertexIndices>();

				for (var i = 0; i < vertexCount; i++)
				{
					vertices.Add(new JVector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
				}

				for (var i = 0; i < indexCount; i += 3)
				{
					var i0 = reader.ReadInt32();
					var i1 = reader.ReadInt32();
					var i2 = reader.ReadInt32();

					indices.Add(new TriangleVertexIndices(i0, i2, i1));
				}

				mesh.Build(isConvexHull, vertices, indices);
			}
		}

		public void Unload(object resource)
		{
			var mesh = (Mesh)resource;
		}
	}
}
