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
        private SkeletonInstance _skeletonInstance = null;

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            _skeletonInstance = null;
        }

        protected override void UpdateDerviedMeshSettings()
        {
            base.UpdateDerviedMeshSettings();

            if (_mesh != null)
            {
                _skeletonInstance = new SkeletonInstance(_mesh);
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (_skeletonInstance != null)
            {
                _skeletonInstance.Update(dt);
            }
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
