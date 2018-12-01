using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Shapes
{
    public class MeshColliderShape : IColliderShape
    {
        public Resources.Mesh Mesh { get; set; }
    }
}
