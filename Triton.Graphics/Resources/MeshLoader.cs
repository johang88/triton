using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	/// <summary>
	/// File format specification:
	/// 
	/// char[] Magic = "MESH"
	/// int Version = 0x0110
	/// int MeshCount
	/// 
	/// Mesh[MeshCount] Meshes
	///		int TriangleCount
	///		int VertexCount // Vertex data size in bytes
	///		int IndexCount // Index data size in bytes
	///		int VertexFormatElementsCount
	/// 
	///		VertexFormatElement[VertexFormatElementsCount] VertexFormatELements
	///			byte Semantic
	///			int Type
	///			byte Count
	///			short Offset
	/// 
	///		byte[VertexCount] VertexData
	///		byte[IndexCount] IndexData
	/// </summary>
	class MeshLoader : Triton.Common.IResourceLoader<Mesh>
	{
		static readonly char[] Magic = new char[] { 'M', 'E', 'S', 'H' };
		const int Version_1 = 0x0100;
		const int Version_1_1 = 0x0110;

		private readonly Backend Backend;
		private readonly Triton.Common.IO.FileSystem FileSystem;

		public MeshLoader(Backend backend, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Backend = backend;
			FileSystem = fileSystem;
		}

		public Common.Resource Create(string name, string parameters)
		{
			return new Mesh(name, parameters);
		}

		public void Load(Common.Resource resource, string parameters, Action<Common.Resource> onLoaded)
		{
			// Destroy any existing mesh handles
			Unload(resource);

			var mesh = (Mesh)resource;

			var filename = resource.Name + ".mesh";

			using (var stream = FileSystem.OpenRead(filename))
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var magic = reader.ReadChars(4);
				for (var i = 0; i < Magic.Length; i++)
				{
					if (magic[i] != Magic[i])
						throw new ArgumentException("invalid mesh");
				}

				var version = reader.ReadInt32();

				var validVersions = new int[] { Version_1, Version_1_1 };

				if (!validVersions.Contains(version))
					throw new ArgumentException("invalid mesh, unknown version");

				var meshCount = reader.ReadInt32();
				mesh.Handles = new int[meshCount];

				var resourcesToLoad = meshCount;
				Renderer.RenderSystem.OnLoadedCallback onResourceLoaded = (handle, success, errors) =>
				{
					resourcesToLoad--;
					Common.Log.WriteLine(errors, success ? Common.LogLevel.Default : Common.LogLevel.Error);

					if (resourcesToLoad > 0)
						return;

					if (onLoaded != null)
						onLoaded(resource);
				};

				for (var i = 0; i < meshCount; i++)
				{
					var triangleCount = reader.ReadInt32();
					var vertexCount = reader.ReadInt32();
					var indexCount = reader.ReadInt32();

					Renderer.VertexFormat vertexFormat;
					if (version >= Version_1_1)
					{
						var vertexFormatElementCount = reader.ReadInt32();

						var vertexFormatElements = new List<Renderer.VertexFormatElement>();
						for (var j = 0; j < vertexFormatElementCount; j++)
						{
							vertexFormatElements.Add(new Renderer.VertexFormatElement(
								(Renderer.VertexFormatSemantic)reader.ReadByte(),
								(Renderer.VertexPointerType)reader.ReadInt32(),
								reader.ReadByte(),
								reader.ReadInt16()
							));
						}

						vertexFormat = new Renderer.VertexFormat(vertexFormatElements.ToArray());
					}
					else
					{
						vertexFormat = new Renderer.VertexFormat(new Renderer.VertexFormatElement[]
						{
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Position, Renderer.VertexPointerType.Float, 3, 0),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Normal, Renderer.VertexPointerType.Float, 3, sizeof(float) * 3),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.Tangent, Renderer.VertexPointerType.Float, 3, sizeof(float) * 6),
							new Renderer.VertexFormatElement(Renderer.VertexFormatSemantic.TexCoord, Renderer.VertexPointerType.Float, 2, sizeof(float) * 9),
						});
					}

					var vertices = reader.ReadBytes(vertexCount);
					var indices = reader.ReadBytes(indexCount);

					mesh.Handles[i] = Backend.RenderSystem.CreateMesh(triangleCount, vertexFormat, vertices, indices, false, onResourceLoaded);
				}

				resource.Parameters = parameters;
			}
		}

		public void Unload(Common.Resource resource)
		{
			var mesh = (Mesh)resource;
			foreach (var handle in mesh.Handles)
			{
				Backend.RenderSystem.DestroyMesh(handle);
			}
			mesh.Handles = null;
		}
	}
}
