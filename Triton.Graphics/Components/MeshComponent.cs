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

        protected virtual void UpdateDerviedMeshSettings()
        {
            if (_mesh == null)
                return;

            for (var i = 0; i < _mesh.SubMeshes.Length; i++)
            {
                if (_mesh.SubMeshes[i].BoundingSphereRadius > _boundingSphere)
                {
                    _boundingSphere = _mesh.SubMeshes[i].BoundingSphereRadius;
                }
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (_meshDirty)
            {
                UpdateDerviedMeshSettings();
                _meshDirty = false;
            }
        }

        public override void PrepareRenderOperations(RenderOperations operations)
        {
            if (_mesh == null)
                return;

            Owner.GetWorldMatrix(out var world);

            for (var i = 0; i < Mesh.SubMeshes.Length; i++)
            {
                var subMesh = Mesh.SubMeshes[i];
                operations.Add(subMesh.Handle, world, subMesh.Material, null, false, CastShadows);
            }
        }
    }
}
