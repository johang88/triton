using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Physics.Shapes
{
    public class BoxColliderShape : IColliderShape
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }
    }
}
