using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Triton.Common
{
	/// <summary>
	/// Manages all the various resources in the engine
	/// 
	/// All resources are resource counted, any resource with a reference count == 0 is eligable for unloading.
	/// Unloaded resources are usually put on the loading queue the next time they are used. 
	/// 
	/// It is also possible to force unload of a single resource or of all resource with referenceCount &lt;= n.
	/// 
	/// The IResourceLoader interface is used to construct new resource loaders.
	/// 
	/// Actual loading of resources is done in async, the actual implementation is transparent to the resource manager.
	/// A method to add items to a work queue is all that has to be provided to the resource manager. It is recommended
	/// that all resource work items on the queue are processed synchroniously, preferably in a background thread.
	/// 
	/// Care has to be taken for threading issues in the resource loaders, for example, opengl resources has to be created
	/// on a valid context, that is active on the processing thread.
	/// 
	/// 
	/// Future features:
	///		Per resource type memory budgets
	/// </summary>
	public class ResourceManager : IDisposable
	{
		private readonly ConcurrentDictionary<string, Resource> Resources = new ConcurrentDictionary<string, Resource>();
		private readonly Dictionary<Type, IResourceLoader> ResourceLoaders = new Dictionary<Type, IResourceLoader>();
		private bool Disposed = false;
		private readonly Action<Action> AddItemToWorkQueue;
		private object LoadingLock = new object();

		public ResourceManager(Action<Action> addItemToWorkQueue)
		{
			AddItemToWorkQueue = addItemToWorkQueue;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (!isDisposing || Disposed)
				return;

			foreach (var resource in Resources.Values)
			{
				Unload(resource, false);
			}

			Disposed = true;
		}

		public TResource Load<TResource>(string name, string parameters = "", Action<Resource> onLoaded = null) where TResource : Resource
		{
			if (!ResourceLoaders.ContainsKey(typeof(TResource)))
				throw new InvalidOperationException("no resource loader for the specified type");

			var identifier = name + "?" + parameters;

			lock (LoadingLock)
			{
				var loader = ResourceLoaders[typeof(TResource)];

				// Get or create the resource
				Resource resource = null;
				if (!Resources.TryGetValue(identifier, out resource))
				{
					resource = loader.Create(name, parameters);
					Resources.AddOrUpdate(identifier, resource, (key, existingVal) => existingVal);
				}

				resource.ReferenceCount += 1;

				// Load the resource if neccecary
				if (resource.State == ResourceLoadingState.Unloaded)
				{
					resource.State = ResourceLoadingState.Loading;

					AddItemToWorkQueue(() =>
					{
						loader.Load(resource, parameters, (r) =>
						{
							resource.State = ResourceLoadingState.Loaded;

							if (!string.IsNullOrWhiteSpace(parameters))
								Log.WriteLine("Loaded {0}?{2} of type {1}", name, typeof(TResource), parameters);
							else
								Log.WriteLine("Loaded {0} of type {1}", name, typeof(TResource));

							if (onLoaded != null)
								onLoaded(r);
						});
					});
				}

				return (TResource)resource;
			}
		}

		public void Unload(Resource resource, bool async = true)
		{
			IResourceLoader loader = null;
			var resourceType = resource.GetType();
			while (loader == null && resourceType != typeof(object))
			{
				if (ResourceLoaders.ContainsKey(resourceType))
				{
					loader = ResourceLoaders[resourceType];
					break;
				}

				resourceType = resourceType.BaseType;
			}

			if (loader == null)
				throw new InvalidOperationException("no resource loader for the specified type");

			lock (LoadingLock)
			{
				if (resource.State == ResourceLoadingState.Loaded)
				{
					resource.State = ResourceLoadingState.Unloading;

					Action unloadAction = () =>
					{
						loader.Unload(resource);
						resource.State = ResourceLoadingState.Unloaded;
						Log.WriteLine("Unloaded {0} of type {1}", resource.Name, resource.GetType());
					};

					if (async)
						AddItemToWorkQueue(unloadAction);
					else
						unloadAction();
				}
			}
		}

		public void Manage(Resource resource)
		{
			if (resource == null)
				throw new ArgumentNullException("resource");

			lock (LoadingLock)
			{
				Resources.AddOrUpdate(resource.Name, resource, (key, existingVal) => existingVal);
				resource.State = ResourceLoadingState.Loaded;
				resource.ReferenceCount += 1;
			}
		}

		public void Release(Resource resource)
		{
			if (resource.ReferenceCount > 0)
				resource.ReferenceCount -= 1;
		}

		public void AddResourceLoader<TResource>(IResourceLoader<TResource> loader) where TResource : Resource
		{
			if (loader == null)
				throw new ArgumentNullException("loader");

			ResourceLoaders.Add(typeof(TResource), loader);
		}

		public void UnloadUnusedResources()
		{
			foreach (var resource in Resources.Where(r => r.Value.ReferenceCount == 0 && r.Value.State == ResourceLoadingState.Loaded))
			{
				Unload(resource.Value);
			}
		}

		public bool AllResourcesLoaded()
		{
			return Resources.All(r => r.Value.State == ResourceLoadingState.Loaded);
		}
	}
}
