using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.Resources;

namespace Triton.Graphics.Components
{
    public class MeshComponent : RenderableComponent
    {
        protected bool _meshDirty = false;

        // Used for world space transform
        private BoundingSphere _boundingSphereLocalSpace;

        protected Mesh _mesh;
        [DataMember]
        public Mesh Mesh
        {
            get => _mesh;
            set
            {
                _mesh = value;
                _meshDirty = true;
            }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            UpdateDerviedMeshSettings();
        }

        protected virtual void UpdateDerviedMeshSettings()
        {
            if (_mesh == null)
                return;

            BoundingBox = _mesh.SubMeshes[0].BoundingBox;
            BoundingSphere = _mesh.SubMeshes[0].BoundingSphere;

            for (var i = 1; i < _mesh.SubMeshes.Length; i++)
            {
                BoundingSphere = BoundingSphere.CreateMerged(BoundingSphere, _mesh.SubMeshes[i].BoundingSphere);
                BoundingBox = BoundingBox.CreateMerged(BoundingBox, _mesh.SubMeshes[i].BoundingBox);
            }

            _boundingSphereLocalSpace = BoundingSphere;

            _meshDirty = false;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (_meshDirty)
            {
                UpdateDerviedMeshSettings();
            }

            Owner.GetWorldMatrix(out var world);
            _boundingSphereLocalSpace.Transform(ref world, out BoundingSphere);
        }

        public override void PrepareRenderOperations(BoundingFrustum frustum, RenderOperations operations)
        {
            if (_mesh == null)
                return;

            Owner.GetWorldMatrix(out var world);

            for (var i = 0; i < Mesh.SubMeshes.Length; i++)
            {
                var subMesh = Mesh.SubMeshes[i];

                Mesh.SubMeshes[i].BoundingSphere.Transform(ref world, out var subMeshBoundingSphere);

                if (frustum == null || frustum.Intersects(ref subMeshBoundingSphere))
                {
                    operations.Add(subMesh.Handle, world, subMesh.Material, null, false, CastShadows);
                }
            }
        }
    }
}
