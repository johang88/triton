using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.SkeletalAnimation;

namespace Triton.Graphics.Components
{
    public class SkinnedMeshComponent : MeshComponent
    {
        private SkeletonInstance _skeletonInstance;

        public override void OnActivate()
        {
            base.OnActivate();

            // TODO: Manage dynamic mesh swapping
            _skeletonInstance = new SkeletonInstance(Mesh);
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            _skeletonInstance = null;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            _skeletonInstance.Update(dt);
        }

        public override void PrepareRenderOperations(RenderOperations operations)
        {
            GetWorldMatrix(out var world);

            for (var i = 0; i < Mesh.SubMeshes.Length; i++)
            {
                var subMesh = Mesh.SubMeshes[i];
                operations.Add(subMesh.Handle, world, subMesh.Material, _skeletonInstance, false, CastShadows);
            }
        }
    }
}
