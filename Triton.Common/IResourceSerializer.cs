using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
    /// Manages resource serialization
    /// Have the resource implement IDisposable for cleanup, will be called autoamtically when the resource is unloaded by the resource manager
	/// </summary>
	public interface IResourceSerializer
	{
		string Extension { get; }
		string DefaultFilename { get; }
        bool SupportsStreaming { get; }

		object Create(Type type);

        Task Deserialize(object resource, byte[] data);
        byte[] Serialize(object resource);
    }

	/// <summary>
	/// Generic interface to implement resource loaders, this is the prefered interface to implement
	/// </summary>
	/// <typeparam name="TResource"></typeparam>
	public interface IResourceSerializer<TResource> : IResourceSerializer where TResource : class
	{
	}
}
