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
		const int Version_1_3 = 0x0130;

		private readonly Backend Backend;
		private readonly Triton.Common.IO.FileSystem FileSystem;
		private readonly Triton.Common.ResourceManager ResourceManager;

		public MeshLoader(Backend backend, Triton.Common.ResourceManager resourceManager, Triton.Common.IO.FileSystem fileSystem)
		{
			if (backend == null)
				throw new ArgumentNullException("backend");
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			Backend = backend;
			FileSystem = fileSystem;
			ResourceManager = resourceManager;
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

			var overrideMaterial = parameters;

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

				var validVersions = new int[] { Version_1_3 };

				if (!validVersions.Contains(version))
					throw new ArgumentException("invalid mesh, unknown version");

				var meshCount = reader.ReadInt32();
				mesh.SubMeshes = new SubMesh[meshCount];

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
					var materialName = reader.ReadString();

					if (!string.IsNullOrWhiteSpace(overrideMaterial))
						materialName = overrideMaterial;

					Material material = null;
					if (materialName != "no_material")
						material = ResourceManager.Load<Material>(materialName);

					var triangleCount = reader.ReadInt32();
					var vertexCount = reader.ReadInt32();
					var indexCount = reader.ReadInt32();

					Renderer.VertexFormat vertexFormat;

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

					float boundingSphereRadius = reader.ReadSingle();

					var vertices = reader.ReadBytes(vertexCount);
					var indices = reader.ReadBytes(indexCount);

					mesh.SubMeshes[i] = new SubMesh
					{
						Handle = Backend.RenderSystem.CreateMesh(triangleCount, vertexFormat, vertices, indices, false, onResourceLoaded),
						Material = material,
						BoundingSphereRadius = boundingSphereRadius
					};
				}

				resource.Parameters = parameters;
			}
		}

		public void Unload(Common.Resource resource)
		{
			var mesh = (Mesh)resource;
			foreach (var subMesh in mesh.SubMeshes)
			{
				Backend.RenderSystem.DestroyMesh(subMesh.Handle);

				if (subMesh.Material != null)
				{
					ResourceManager.Unload(subMesh.Material);
					subMesh.Material = null;
				}
			}

			mesh.SubMeshes = null;
		}
	}
}
