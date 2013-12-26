using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class Stage
	{
		private readonly Common.ResourceManager ResourceManager;
		
		private readonly List<MeshInstance> Meshes = new List<MeshInstance>();
		private readonly List<Light> Lights = new List<Light>();

		public Vector3 AmbientColor = new Vector3(0.2f, 0.2f, 0.2f);
		public Vector4 ClearColor = Vector4.Zero;

		public Stage(Common.ResourceManager resourceManager)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;
		}

		public MeshInstance AddMesh(string mesh)
		{
			return AddMesh(ResourceManager.Load<Resources.Mesh>(mesh));
		}

		public MeshInstance AddMesh(Resources.Mesh mesh)
		{
			var instance = new MeshInstance
			{
				Mesh = mesh
			};

			Meshes.Add(instance);

			return instance;
		}

		public void RemoveMesh(MeshInstance mesh)
		{
			Meshes.Remove(mesh);
			ResourceManager.Unload(mesh.Mesh);
		}

		public void Clear()
		{
			Meshes.Clear();
			Lights.Clear();
		}

		public IReadOnlyCollection<MeshInstance> GetMeshes()
		{
			return Meshes;
		}

		public Light CreateDirectionalLight(Vector3 direction, Vector3 color, bool castShadows, float shadowRange = 64.0f, float shadowBias = 0.001f)
		{
			var light = new Light
			{
				Type = LighType.Directional,
				Direction = direction,
				Color = color,
				CastShadows = castShadows,
				ShadowBias = shadowBias,
				Range = shadowRange
			};

			Lights.Add(light);

			return light;
		}

		public Light CreatePointLight(Vector3 position, float range, Vector3 color, bool castShadows, float shadowBias = 0.001f)
		{
			var light = new Light
			{
				Type = LighType.PointLight,
				Position = position,
				Range = range,
				Color = color,
				CastShadows = castShadows,
				ShadowBias = shadowBias
			};

			Lights.Add(light);

			return light;
		}

		public Light CreateSpotLight(Vector3 position, Vector3 direction, float innerAngle, float outerAngle, float range, Vector3 color, bool castShadows, float shadowBias = 0.001f)
		{
			direction.Normalize();
			var light = new Light
			{
				Type = LighType.SpotLight,
				Position = position,
				Range = range,
				Color = color,
				Direction = direction,
				InnerAngle = innerAngle,
				OuterAngle = outerAngle,
				CastShadows = castShadows,
				ShadowBias = shadowBias
			};

			Lights.Add(light);

			return light;
		}

		public void RemoveLight(Light light)
		{
			Lights.Remove(light);
		}

		public IReadOnlyCollection<Light> GetLights()
		{
			return Lights;
		}
	}
}
