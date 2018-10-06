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
        private readonly Dictionary<Type, IResourceSerializer> _resourceSerializers = new Dictionary<Type, IResourceSerializer>();

        private readonly IO.FileSystem _fileSystem;

        private bool _isDispoed = false;
        //private readonly object _loadingLock = new object();
        private SemaphoreSlim _loadingLock = new SemaphoreSlim(1);

        public IResourceSerializer DefaultResourceLoader { get; set; }

        public ResourceManager(IO.FileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            DefaultResourceLoader = new GenericResourceLoader(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing || _isDispoed)
                return;

            foreach (var resource in _resources.Values)
            {
                UnloadResource(resource, false);
            }

            _instanceToReference.Clear();
            _resources.Clear();

            _isDispoed = true;
        }

        public TResource Load<TResource>(string name, string parameters = "") where TResource : class
             => LoadAsync<TResource>(name, parameters).Result;

        public object Load(Type resourceType, string name, string parameters = "")
             => LoadAsync(resourceType, name, parameters).Result;

        public async Task<TResource> LoadAsync<TResource>(string name, string parameters = "") where TResource : class
             => (TResource)(await LoadAsync(typeof(TResource), name, parameters));

        public async Task<object> LoadAsync(Type resourceType, string name, string parameters = "")
        {
            var identifier = name + "?" + parameters;

            // Fetch loader
            IResourceSerializer loader = DefaultResourceLoader;
            if (_resourceSerializers.ContainsKey(resourceType))
                loader = _resourceSerializers[resourceType];

            await _loadingLock.WaitAsync();

            // Get or create the resource
            if (!_resources.TryGetValue(identifier, out var resourceReference))
            {
                resourceReference = new ResourceReference(name, parameters)
                {
                    Resource = loader.Create(resourceType)
                };

                _resources.AddOrUpdate(identifier, resourceReference, (key, existingVal) => existingVal);
                _instanceToReference.AddOrUpdate(resourceReference.Resource, resourceReference, (key, existingVal) => existingVal);
            }

            resourceReference.AddReference();

            // Load the resource if neccecary
            if (resourceReference.State == ResourceLoadingState.Unloaded)
            {
                resourceReference.State = ResourceLoadingState.Loading;
                resourceReference.LoadingTask = LoadResource(resourceReference, loader);
            }

            _loadingLock.Release();
            await resourceReference.LoadingTask;

            return resourceReference.Resource;
        }

        private async Task LoadResource(ResourceReference resource, IResourceSerializer loader)
        {
            var data = await await Concurrency.TaskHelpers.RunOnIOThread(() => LoadDataForResource(resource, loader));
            await await Concurrency.TaskHelpers.RunOnMainThread(() => loader.Load(resource.Resource, data));

            resource.State = ResourceLoadingState.Loaded;

            if (!string.IsNullOrWhiteSpace(resource.Parameters))
                Log.WriteLine("Loaded {0}?{2} of type {1}", resource.Name, resource.Resource.GetType(), resource.Parameters);
            else
                Log.WriteLine("Loaded {0} of type {1}", resource.Name, resource.Resource.GetType());
        }

        private async Task<byte[]> LoadDataForResource(ResourceReference resource, IResourceSerializer loader)
        {
            if (loader.SupportsStreaming)
            {
                return null;
            }

            var path = resource.Name + loader.Extension;

            if (!_fileSystem.FileExists(path) && !string.IsNullOrWhiteSpace(loader.DefaultFilename))
            {
                path = loader.DefaultFilename;
            }

            using (var stream = _fileSystem.OpenRead(path))
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
            _loadingLock.Wait();

            var resourceType = resourceReference.Resource.GetType();

            IResourceSerializer loader = DefaultResourceLoader;
            if (_resourceSerializers.ContainsKey(resourceType))
                loader = _resourceSerializers[resourceType];

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

            _loadingLock.Release();
        }

        public void Manage<TResource>(string name, TResource resource) where TResource : class
        {
            if (resource == null)
                throw new ArgumentNullException("resource");

            _loadingLock.Wait();

            var resourceReference = new ResourceReference(name, null)
            {
                Resource = resource,
                State = ResourceLoadingState.Loaded,
            };

            resourceReference.AddReference();

            _resources.AddOrUpdate(name, resourceReference, (key, existingVal) => existingVal);
            _instanceToReference.AddOrUpdate(resource, resourceReference, (key, existingVal) => existingVal);

            _loadingLock.Release();
        }

        public void Unload<TResource>(TResource resource) where TResource : class
        {
            if (_instanceToReference.TryGetValue(resource, out var resourceReference))
            {
                resourceReference.RemoveReference();
            }
        }

        public bool IsManaged(object resource)
            => _instanceToReference.ContainsKey(resource);

        public void AddResourceLoader<TResource>(IResourceSerializer<TResource> loader) where TResource : class
            => _resourceSerializers.Add(typeof(TResource), loader ?? throw new ArgumentNullException("loader"));

        public void GargabgeCollect()
        {
            foreach (var resource in _resources.Values)
            {
                if (resource.ReferenceCount <= 0)
                {
                    UnloadResource(resource);
                }
            }
        }

        public bool AllResourcesLoaded()
            => _resources.All(r => r.Value.State == ResourceLoadingState.Loaded);

        public (string name, string parameters) GetResourceProperties<TResource>(TResource resource) where TResource : class
        {
            var reference = _instanceToReference[resource];
            return (reference.Name, reference.Parameters);
        }

        private class ResourceReference
        {
            private int _referenceCount = 0;

            public string Name { get; set; }
            public ResourceLoadingState State { get; set; }
            public int ReferenceCount => _referenceCount;
            public string Parameters { get; set; }
            public object Resource { get; set; }
            public Task LoadingTask { get; set; }

            public ResourceReference(string name, string parameters)
            {
                Name = name;
                Parameters = parameters;
            }

            internal void AddReference()
                => Interlocked.Increment(ref _referenceCount);

            internal void RemoveReference()
                => Interlocked.Decrement(ref _referenceCount);
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
