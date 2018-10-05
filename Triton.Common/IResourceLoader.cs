using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
	/// Implementsa resource loader
	/// Resource loaders provide metohds for creating, load and unloading resources.
	/// The actual resource life management is done by the resource management system.
	/// 
	/// Load can be called several times for an already loaded resource, it is up to the resource loader
	/// to decide if the resource has to be reloaded or not.
	/// </summary>
	public interface IResourceLoader
	{
		string Extension { get; }
		string DefaultFilename { get; }
        bool SupportsStreaming { get; }

		object Create(Type type);
		Task Load(object resource, byte[] data);
		void Unload(object resource);
	}

	/// <summary>
	/// Generic interface to implement resource loaders, this is the prefered interface to implement
	/// </summary>
	/// <typeparam name="TResource"></typeparam>
	public interface IResourceLoader<TResource> : IResourceLoader where TResource : class
	{
	}
}
