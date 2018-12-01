using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
    public class Stage
    {
        /// <summary>
        /// Fallback ambient color if no ambient light is set
        /// </summary>
        public Vector3 AmbientColor = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector4 ClearColor = Vector4.Zero;

        private BoundingFrustum Frustum = new BoundingFrustum(Matrix4.Identity);

        public AmbientLight AmbientLight { get; set; }

        public Camera Camera { get; set; }

        private List<Components.RenderableComponent> _renderableComponents = new List<Components.RenderableComponent>();
        private List<Components.LightComponent> _lightComponents = new List<Components.LightComponent>();

        internal void AddRenderableComponent(Components.RenderableComponent component)
            => _renderableComponents.Add(component);

        internal void RemoveRenderableComponent(Components.RenderableComponent component)
            => _renderableComponents.Remove(component);

        internal void AddLightComponent(Components.LightComponent component)
            => _lightComponents.Add(component);

        internal void RemoveLightComponent(Components.LightComponent component)
            => _lightComponents.Remove(component);

        public void PrepareRenderOperations(Matrix4 viewMatrix, RenderOperations operations, bool shadowCastersOnly = false, bool frustumCull = true)
        {
            // TODO: Frustum cull!
            for (var i = 0; i < _renderableComponents.Count; i++)
            {
                if (!shadowCastersOnly || _renderableComponents[i].CastShadows)
                {
                    _renderableComponents[i].PrepareRenderOperations(operations);
                }
            }
            //Frustum.Matrix = viewMatrix;
            //var sphere = new BoundingSphere();
            //var zero = Vector3.Zero;

            //for (var i = 0; i < _meshes.Count; i++)
            //{
            //    var meshInstance = _meshes[i];
            //    var subMeshes = meshInstance.Mesh.SubMeshes;

            //    if (!meshInstance.CastShadows && shadowCastersOnly)
            //        continue;

            //    Vector3.Transform(ref zero, ref meshInstance.World, out sphere.Center);

            //    for (var j = 0; j < subMeshes.Length; j++)
            //    {
            //        var subMesh = subMeshes[j];

            //        sphere.Radius = subMesh.BoundingSphereRadius;
            //        if (!frustumCull || Frustum.Intersects(sphere))
            //        {
            //            operations.Add(subMesh.Handle, meshInstance.World, subMesh.Material, meshInstance.Skeleton, false, meshInstance.CastShadows);
            //        }
            //    }
            //}
        }

        public Components.LightComponent GetSunLight()
        {
            foreach (var light in _lightComponents)
            {
                if (light.Type == LighType.Directional)
                    return light;
            }

            return null;
        }

        public IReadOnlyCollection<Components.LightComponent> GetLights()
        {
            return _lightComponents;
        }
    }
}
