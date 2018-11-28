using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
    public class Stage
    {
        private readonly Triton.Resources.ResourceManager _resourceManager;

        private readonly List<MeshInstance> _meshes = new List<MeshInstance>();
        private readonly List<Light> _lights = new List<Light>();

        /// <summary>
        /// Fallback ambient color if no ambient light is set
        /// </summary>
        public Vector3 AmbientColor = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector4 ClearColor = Vector4.Zero;

        private BoundingFrustum Frustum = new BoundingFrustum(Matrix4.Identity);

        public Light SunLight { get; private set; }

        public AmbientLight AmbientLight { get; set; }

        public Camera Camera { get; set; }

        public Stage(Triton.Resources.ResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
        }

        public MeshInstance AddMesh(string mesh)
        {
            return AddMesh(_resourceManager.Load<Resources.Mesh>(mesh));
        }

        public MeshInstance AddMesh(Resources.Mesh mesh)
        {
            var instance = new MeshInstance
            {
                Mesh = mesh
            };

            _resourceManager.AddReference(mesh);
            _meshes.Add(instance);

            return instance;
        }

        public void RemoveMesh(MeshInstance mesh)
        {
            _meshes.Remove(mesh);
            _resourceManager.Unload(mesh.Mesh);
        }

        public void Clear()
        {
            _meshes.Clear();
            _lights.Clear();
        }

        public void PrepareRenderOperations(Matrix4 viewMatrix, RenderOperations operations, bool shadowCastersOnly = false, bool frustumCull = true)
        {
            Frustum.Matrix = viewMatrix;
            var sphere = new BoundingSphere();
            var zero = Vector3.Zero;

            for (var i = 0; i < _meshes.Count; i++)
            {
                var meshInstance = _meshes[i];
                var subMeshes = meshInstance.Mesh.SubMeshes;

                if (!meshInstance.CastShadows && shadowCastersOnly)
                    continue;

                Vector3.Transform(ref zero, ref meshInstance.World, out sphere.Center);

                for (var j = 0; j < subMeshes.Length; j++)
                {
                    var subMesh = subMeshes[j];

                    sphere.Radius = subMesh.BoundingSphereRadius;
                    if (!frustumCull || Frustum.Intersects(sphere))
                    {
                        operations.Add(subMesh.Handle, meshInstance.World, subMesh.Material, meshInstance.Skeleton, false, meshInstance.CastShadows);
                    }
                }
            }
        }

        public void PrepareRenderOperations(Vector3 position, float radius, RenderOperations operations, bool shadowCastersOnly = false)
        {
            var zero = Vector3.Zero;
            Vector3 meshPosition;

            for (var i = 0; i < _meshes.Count; i++)
            {
                var meshInstance = _meshes[i];
                var subMeshes = meshInstance.Mesh.SubMeshes;

                Vector3.Transform(ref zero, ref meshInstance.World, out meshPosition);

                if ((!shadowCastersOnly || meshInstance.CastShadows) && Math.Intersections.SphereToSphere(ref position, radius, ref meshPosition, meshInstance.Mesh.BoundingSphereRadius))
                {
                    for (var j = 0; j < subMeshes.Length; j++)
                    {
                        var subMesh = subMeshes[j];
                        operations.Add(subMesh.Handle, meshInstance.World, subMesh.Material, meshInstance.Skeleton, false, meshInstance.CastShadows);
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

            _lights.Add(light);
            if (SunLight == null)
            {
                SunLight = light;
            }

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

            _lights.Add(light);

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

            _lights.Add(light);

            return light;
        }

        public void RemoveLight(Light light)
        {
            if (SunLight == light)
            {
                SunLight = null;
            }

            _lights.Remove(light);
        }

        public Light GetSunLight()
        {
            foreach (var light in _lights)
            {
                if (light.Type == LighType.Directional)
                    return light;
            }

            return null;
        }

        public IReadOnlyCollection<Light> GetLights()
        {
            return _lights;
        }
    }
}
