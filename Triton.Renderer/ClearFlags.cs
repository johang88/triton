using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
    [Flags]
    public enum ClearFlags : byte
    {
        None = 0x0,
        Depth = 0x01,
        Color = 0x02,
        All = Depth | Color
    }
}
