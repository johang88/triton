using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Skeletons
{
	interface ISkeletonConverter
	{
		Skeleton Import(System.IO.Stream stream);
	}
}
