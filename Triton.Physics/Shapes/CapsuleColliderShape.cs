using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Shapes
{
    public class CapsuleColliderShape : IColliderShape
    {
        public float Radius { get; set; }
        public float Height { get; set; }
    }
}
