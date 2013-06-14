using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Meshes
{
	interface IMeshImporter
	{
		Mesh Import(System.IO.Stream stream);
	}
}
