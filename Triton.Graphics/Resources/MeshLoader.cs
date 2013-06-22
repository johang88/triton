using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	class MeshLoader : Triton.Common.IResourceLoader<Mesh>
	{
		static readonly char[] Magic = new char[] { 'M', 'E', 'S', 'H' };
		const int Version = 0x0100;

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
				if (version != Version)
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

					var vertices = reader.ReadBytes(vertexCount);
					var indices = reader.ReadBytes(indexCount);

					mesh.Handles[i] = Backend.RenderSystem.CreateMesh(triangleCount, vertices, indices, false, onResourceLoaded);
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
