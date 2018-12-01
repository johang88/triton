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

        internal void GetWorldMatrix(out Matrix4 world)
        {
            var scale = Matrix4.Scale(Owner.Scale);
            Matrix4.Rotate(ref Owner.Orientation, out var rotation);
            Matrix4.CreateTranslation(ref Owner.Position, out var translation);

            Matrix4.Mult(ref scale, ref rotation, out var rotationScale);
            Matrix4.Mult(ref rotationScale, ref translation, out world);
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

            GetWorldMatrix(out var world);

            for (var i = 0; i < Mesh.SubMeshes.Length; i++)
            {
                var subMesh = Mesh.SubMeshes[i];
                operations.Add(subMesh.Handle, world, subMesh.Material, null, false, CastShadows);
            }
        }
    }
}
