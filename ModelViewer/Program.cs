using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelViewer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var app = new Application())
            {
                app.Run();
            }
        }
    }
}
