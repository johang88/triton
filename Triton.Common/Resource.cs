using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
	/// Resource reference
	/// Has to be implemented together with a resource loader in order to be useful.
	/// </summary>
	public abstract class Resource
	{
		public readonly string Name;
		public bool IsLoaded { get; set; }
		public int ReferenceCount { get; internal set; }
		public string Parameters { get; set; }

		public Resource(string name, string parameters)
		{
			Name = name;
			Parameters = parameters;
		}
	}
}
