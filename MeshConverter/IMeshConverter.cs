using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshConverter
{
	interface IMeshImporter
	{
		Mesh Import(System.IO.Stream stream);
	}
}
