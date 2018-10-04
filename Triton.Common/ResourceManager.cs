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
	/// Future features:
	///		Per resource type memory budgets
	/// </summary>
	public class ResourceManager : IDisposable
	{
		private readonly ConcurrentDictionary<string, ResourceReference> _resources = new ConcurrentDictionary<string, ResourceReference>();
        private readonly ConcurrentDictionary<object, ResourceReference> _instanceToReference = new ConcurrentDictionary<object, ResourceReference>();
		private readonly Dictionary<Type, IResourceLoader> _resourceLoaders = new Dictionary<Type, IResourceLoader>();
		private readonly ConcurrentQueue<ResourceToLoad> _resourcesToLoad = new ConcurrentQueue<ResourceToLoad>();

		private readonly IO.FileSystem FileSystem;

		private bool Disposed = false;
		private object LoadingLock = new object();

		public ResourceManager(IO.FileSystem fileSystem)
		{
            FileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
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

			foreach (var resource in _resources.Values)
			{
				UnloadResource(resource, false);
			}

            _instanceToReference.Clear();
            _resources.Clear();

            Disposed = true;
		}

		public TResource Load<TResource>(string name, string parameters = "") where TResource : class
		{
			if (!_resourceLoaders.ContainsKey(typeof(TResource)))
				throw new InvalidOperationException("no resource loader for the specified type");

			var identifier = name + "?" + parameters;

			lock (LoadingLock)
			{
				var loader = _resourceLoaders[typeof(TResource)];

                // Get or create the resource
                if (!_resources.TryGetValue(identifier, out var resourceReference))
                {
                    resourceReference = new ResourceReference(name, parameters);
                    resourceReference.Resource = loader.Create(name, parameters);

                    _resources.AddOrUpdate(identifier, resourceReference, (key, existingVal) => existingVal);
                    _instanceToReference.AddOrUpdate(resourceReference.Resource, resourceReference, (key, existingVal) => existingVal);
                }

                resourceReference.ReferenceCount += 1;

				// Load the resource if neccecary
				if (resourceReference.State == ResourceLoadingState.Unloaded)
				{
                    resourceReference.State = ResourceLoadingState.Loading;

					Task.Factory.StartNew(async () =>
					{
						await LoadResource(resourceReference, loader);
					});
				}

				return resourceReference.Resource as TResource;
			}
		}

		private async Task LoadResource(ResourceReference resource, IResourceLoader loader)
		{
			var data = await LoadDataForResource(resource, loader);
			_resourcesToLoad.Enqueue(new ResourceToLoad
			{
				ResourceReference = resource,
				Loader = loader,
				Data = data
			});
		}

		private async Task<byte[]> LoadDataForResource(ResourceReference resource, IResourceLoader loader)
		{
            if (loader.SupportsStreaming)
            {
                return null;
            }

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

		private void UnloadResource(ResourceReference resourceReference, bool async = true)
		{
			IResourceLoader loader = null;
			var resourceType = resourceReference.Resource.GetType();
			while (loader == null && resourceType != typeof(object))
			{
				if (_resourceLoaders.ContainsKey(resourceType))
				{
					loader = _resourceLoaders[resourceType];
					break;
				}

				resourceType = resourceType.BaseType;
			}

			if (loader == null)
				throw new InvalidOperationException("no resource loader for the specified type");

			lock (LoadingLock)
			{
				if (resourceReference.State == ResourceLoadingState.Loaded)
				{
					resourceReference.State = ResourceLoadingState.Unloading;

                    void unloadAction()
                    {
                        loader.Unload(resourceReference.Resource);
                        resourceReference.State = ResourceLoadingState.Unloaded;
                        Log.WriteLine("Unloaded {0} of type {1}", resourceReference.Name, resourceReference.GetType());
                    }

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
			while (maxResourcesPerFrame > 0 && _resourcesToLoad.Count > 0)
			{
				if (_resourcesToLoad.TryDequeue(out var resourceToLoad))
				{
					resourceToLoad.Loader.Load(resourceToLoad.ResourceReference.Resource, resourceToLoad.Data);
					resourceToLoad.ResourceReference.State = ResourceLoadingState.Loaded;

					if (!string.IsNullOrWhiteSpace(resourceToLoad.ResourceReference.Parameters))
						Log.WriteLine("Loaded {0}?{2} of type {1}", resourceToLoad.ResourceReference.Name, resourceToLoad.ResourceReference.GetType(), resourceToLoad.ResourceReference.Parameters);
					else
						Log.WriteLine("Loaded {0} of type {1}", resourceToLoad.ResourceReference.Name, resourceToLoad.ResourceReference.GetType());
				}
			}
		}

		public void Manage<TResource>(string name, TResource resource) where TResource : class
        {
			if (resource == null)
				throw new ArgumentNullException("resource");

			lock (LoadingLock)
			{
                var resourceReference = new ResourceReference(name, null)
                {
                    Resource = resource,
                    State = ResourceLoadingState.Loaded,
                    ReferenceCount = 1
                };

                _resources.AddOrUpdate(name, resourceReference, (key, existingVal) => existingVal);
                _instanceToReference.AddOrUpdate(resource, resourceReference, (key, existingVal) => existingVal);
			}
		}

		public void Unload<TResource>(TResource resource) where TResource : class
        {
            if (_instanceToReference.TryGetValue(resource, out var resourceReference))
            {
                resourceReference.ReferenceCount -= 1;
            }
		}

		public void AddResourceLoader<TResource>(IResourceLoader<TResource> loader) where TResource : class
		{
			if (loader == null)
				throw new ArgumentNullException("loader");

			_resourceLoaders.Add(typeof(TResource), loader);
		}

		public void UnloadUnusedResources()
		{
			foreach (var resource in _resources.Where(r => r.Value.ReferenceCount == 0 && r.Value.State == ResourceLoadingState.Loaded))
			{
				UnloadResource(resource.Value);
			}
		}

		public bool AllResourcesLoaded()
            => _resources.All(r => r.Value.State == ResourceLoadingState.Loaded);

		struct ResourceToLoad
		{
			public ResourceReference ResourceReference;
			public IResourceLoader Loader;
			public byte[] Data;
		}

        private class ResourceReference
        {
            public string Name { get; set; }
            public ResourceLoadingState State { get; set; }
            public int ReferenceCount { get; set; }
            public string Parameters { get; set; }
            public object Resource { get; set; }

            public ResourceReference(string name, string parameters)
            {
                Name = name;
                Parameters = parameters;
            }
        }

        public enum ResourceLoadingState
        {
            Unloaded,
            Loading,
            Loaded,
            Unloading
        }
    }
}
