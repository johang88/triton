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
		public Vector4 ClearColor = Vector4.Zero;

		private BoundingFrustum Frustum = new BoundingFrustum(Matrix4.Identity);

		public Stage(Common.ResourceManager resourceManager)
		{
			if (resourceManager == null)
				throw new ArgumentNullException("resourceManager");

			ResourceManager = resourceManager;
		}

		public MeshInstance AddMesh(string mesh, string parameters = "")
		{
			return AddMesh(ResourceManager.Load<Resources.Mesh>(mesh, parameters));
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

		public void PrepareRenderOperations(Matrix4 viewMatrix, RenderOperations operations)
		{
			Frustum.Matrix = viewMatrix;
			var sphere = new BoundingSphere();

			for (var i = 0; i < Meshes.Count; i++)
			{
				var meshInstance = Meshes[i];
				var subMeshes = meshInstance.Mesh.SubMeshes;

				sphere.Center = Vector3.Transform(Vector3.Zero, meshInstance.World);

				for (var j = 0; j < subMeshes.Length; j++)
				{
					var subMesh = subMeshes[j];

					sphere.Radius = subMesh.BoundingSphereRadius;
					if (Frustum.Intersects(sphere))
					{
						operations.Add(subMesh.Handle, meshInstance.World, subMesh.Material, null, false, meshInstance.CastShadows);
					}
				}
			}
		}

		public void PrepareRenderOperations(Vector3 position, float radius, RenderOperations operations, bool shadowCastersOnly = false)
		{
			for (var i = 0; i < Meshes.Count; i++)
			{
				var meshInstance = Meshes[i];
				var subMeshes = meshInstance.Mesh.SubMeshes;
				var meshPosition = Vector3.Transform(Vector3.Zero, meshInstance.World);

				if ((!shadowCastersOnly || meshInstance.CastShadows) && Math.Intersections.SphereToSphere(ref position, radius, ref meshPosition, meshInstance.Mesh.BoundingSphereRadius))
				{
					for (var j = 0; j < subMeshes.Length; j++)
					{
						var subMesh = subMeshes[j];
						operations.Add(subMesh.Handle, meshInstance.World, subMesh.Material, null, false, meshInstance.CastShadows);
					}
				}
			}
		}

		public Light CreateDirectionalLight(Vector3 direction, Vector3 color, bool castShadows = false, float shadowRange = 64.0f, float shadowBias = 0.001f, float intensity = 1.0f)
		{
			var light = new Light
			{
				Type = LighType.Directional,
				Direction = direction,
				Color = color,
				CastShadows = castShadows,
				ShadowBias = shadowBias,
				Range = shadowRange,
				Intensity = intensity
			};

			Lights.Add(light);

			return light;
		}

		public Light CreatePointLight(Vector3 position, float range, Vector3 color, bool castShadows = false, float shadowBias = 0.001f, float intensity = 1.0f)
		{
			var light = new Light
			{
				Type = LighType.PointLight,
				Position = position,
				Range = range,
				Color = color,
				CastShadows = castShadows,
				ShadowBias = shadowBias,
				Intensity = intensity
			};

			Lights.Add(light);

			return light;
		}

		public Light CreateSpotLight(Vector3 position, Vector3 direction, float innerAngle, float outerAngle, float range, Vector3 color, bool castShadows = false, float shadowBias = 0.001f, float intensity = 1.0f)
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
				ShadowBias = shadowBias,
				Intensity = intensity
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
