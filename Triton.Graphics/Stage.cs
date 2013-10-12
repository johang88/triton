﻿using System;
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

		public Stage(Common.ResourceManager resourceManager)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;
		}

		public MeshInstance AddMesh(string mesh, string material)
		{
			return AddMesh(ResourceManager.Load<Resources.Mesh>(mesh), ResourceManager.Load<Resources.Material>(material));
		}

		public MeshInstance AddMesh(Resources.Mesh mesh, Resources.Material material)
		{
			var instance = new MeshInstance
			{
				Mesh = mesh,
				Material = material,
				Orientation = Quaternion.Identity,
				Position = Vector3.Zero
			};

			Meshes.Add(instance);

			return instance;
		}

		public void RemoveMesh(MeshInstance mesh)
		{
			Meshes.Remove(mesh);
		}

		public IReadOnlyCollection<MeshInstance> GetMeshes()
		{
			return Meshes;
		}

		public Light CreatePointLight(Vector3 position, float range, Vector3 color)
		{
			var light = new Light
			{
				Type = LighType.PointLight,
				Position = position,
				Range = range,
				Color = color
			};

			Lights.Add(light);

			return light;
		}

		public Light CreateSpotLight(Vector3 position, Vector3 direction, float innerAngle, float outerAngle, float range, Vector3 color)
		{
			var light = new Light
			{
				Type = LighType.SpotLight,
				Position = position,
				Range = range,
				Color = color,
				Direction = direction,
				InnerAngle = innerAngle,
				OuterAngle = outerAngle
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