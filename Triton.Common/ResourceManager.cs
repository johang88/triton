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
	/// All resources are reference counted, any resource with a reference count == 0 is eligable for unloading.
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
		private readonly ConcurrentQueue<ResourceToLoad> ResourcesToLoad = new ConcurrentQueue<ResourceToLoad>();

		private readonly IO.FileSystem FileSystem;

		private bool Disposed = false;
		private object LoadingLock = new object();

		public ResourceManager(IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
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
				UnloadResource(resource, false);
			}

			Disposed = true;
		}

		public TResource Load<TResource>(string name, string parameters = "") where TResource : Resource
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

					Task.Factory.StartNew(async () =>
					{
						await LoadResource(resource, loader);
					});
				}

				return (TResource)resource;
			}
		}

		private async Task LoadResource(Resource resource, IResourceLoader loader)
		{
			var data = await LoadDataForResource(resource, loader);
			ResourcesToLoad.Enqueue(new ResourceToLoad
			{
				Resource = resource,
				Loader = loader,
				Data = data
			});
		}

		private async Task<byte[]> LoadDataForResource(Resource resource, IResourceLoader loader)
		{
			var path = resource.Name + loader.Extension;

			if (!FileSystem.FileExists(path) && !string.IsNullOrWhiteSpace(loader.DefaultFilename))
			{
				path = loader.DefaultFilename;
			}

			using (var stream = FileSystem.OpenRead(path))
			{
				byte[] data = new byte[stream.Length];

				long bytesRead = 0;
				while (bytesRead < stream.Length)
				{
					bytesRead += await stream.ReadAsync(data, (int)bytesRead, (int)(stream.Length - bytesRead));
				}

				return data;
			}
		}

		private void UnloadResource(Resource resource, bool async = true)
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

					var task = new Task(unloadAction);
					if (async)
						task.Start();
					else
						task.RunSynchronously();
				}
			}
		}

		/// <summary>
		/// Should be called on the main thread
		/// </summary>
		public void TickResourceLoading(int maxResourcesPerFrame = 10)
		{
			while (maxResourcesPerFrame > 0 && ResourcesToLoad.Count > 0)
			{
				ResourceToLoad resourceToLoad;
				if (ResourcesToLoad.TryDequeue(out resourceToLoad))
				{
					resourceToLoad.Loader.Load(resourceToLoad.Resource, resourceToLoad.Data);
					resourceToLoad.Resource.State = ResourceLoadingState.Loaded;

					if (!string.IsNullOrWhiteSpace(resourceToLoad.Resource.Parameters))
						Log.WriteLine("Loaded {0}?{2} of type {1}", resourceToLoad.Resource.Name, resourceToLoad.Resource.GetType(), resourceToLoad.Resource.Parameters);
					else
						Log.WriteLine("Loaded {0} of type {1}", resourceToLoad.Resource.Name, resourceToLoad.Resource.GetType());
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

		public void Unload(Resource resource)
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
				UnloadResource(resource.Value);
			}
		}

		public bool AllResourcesLoaded()
		{
			return Resources.All(r => r.Value.State == ResourceLoadingState.Loaded);
		}

		struct ResourceToLoad
		{
			public Resource Resource;
			public IResourceLoader Loader;
			public byte[] Data;
		}
	}
}
