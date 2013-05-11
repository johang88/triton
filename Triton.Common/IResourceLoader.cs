using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	public interface IResourceLoader
	{
		Resource Create(string name, string parameters);
		void Load(Resource resource, string parameters);
		void Unload(Resource resource);
	}

	public interface IResourceLoader<TResource> : IResourceLoader where TResource : Resource
	{
	}
}
