using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Triton.Logging;

namespace Triton.Resources
{
    public class ResourceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ResourceReference> _resources = new ConcurrentDictionary<string, ResourceReference>();
        private readonly ConcurrentDictionary<object, ResourceReference> _instanceToReference = new ConcurrentDictionary<object, ResourceReference>();
        private readonly Dictionary<Type, IResourceSerializer> _resourceSerializers = new Dictionary<Type, IResourceSerializer>();
        private readonly List<ResourceReference> _resourcesToUnload = new List<ResourceReference>();

        public HashSet<Type> KnownResourceTypes = new HashSet<Type>();

        private readonly IO.FileSystem _fileSystem;

        private bool _isDispoed = false;
        //private readonly object _loadingLock = new object();
        private SemaphoreSlim _loadingLock = new SemaphoreSlim(1);

        public IResourceSerializer DefaultResourceSerializer { get; set; }

        public ResourceManager(IO.FileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
            DefaultResourceSerializer = new GenericResourceSerializer(this);
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

        /// <summary>
        /// Increase the reference count to a resource, this must be followed by a call to Unload,
        /// can be used to keep resources alive and well
        /// </summary>
        /// <param name="resource"></param>
        public void AddReference(object resource)
        {
            if (_instanceToReference.TryGetValue(resource, out var resourceReference))
                resourceReference.AddReference();
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

            // Fetch serializer
            IResourceSerializer serializer = DefaultResourceSerializer;
            if (_resourceSerializers.ContainsKey(resourceType))
                serializer = _resourceSerializers[resourceType];
            
            await _loadingLock.WaitAsync();

            KnownResourceTypes.Add(resourceType);

            // Get or create the resource
            if (!_resources.TryGetValue(identifier, out var resourceReference))
            {
                resourceReference = new ResourceReference(name, parameters)
                {
                    Resource = serializer.Create(resourceType)
                };

                _resources.AddOrUpdate(identifier, resourceReference, (key, existingVal) => existingVal);
                _instanceToReference.AddOrUpdate(resourceReference.Resource, resourceReference, (key, existingVal) => existingVal);
            }

            resourceReference.AddReference();

            // Load the resource if neccecary
            if (resourceReference.State == ResourceLoadingState.Unloaded)
            {
                resourceReference.State = ResourceLoadingState.Loading;
                resourceReference.LoadingTask = LoadResource(resourceReference, serializer);
            }

            _loadingLock.Release();
            await resourceReference.LoadingTask;

            return resourceReference.Resource;
        }

        /// <summary>
        /// Try to safe a resource to (Name + Extension)
        /// </summary>
        /// <param name="resource"></param>
        public void Save(object resource)
        {
            var (name, _) = GetResourceProperties(resource);

            Save(name, resource);
        }

        public void Save(string name, object resource)
        {
            var resourceType = resource.GetType();

            // Fetch serializer
            IResourceSerializer serializer = DefaultResourceSerializer;
            if (_resourceSerializers.ContainsKey(resourceType))
                serializer = _resourceSerializers[resourceType];

            // Serialize to byte array
            var data = serializer.Serialize(resource);

            var path = name + serializer.Extension;

            using (var stream = _fileSystem.OpenWrite(path))
            {
                stream.Write(data);
            }
        }

        private async Task LoadResource(ResourceReference resource, IResourceSerializer serializer)
        {
            var data = await await Concurrency.TaskHelpers.RunOnIOThread(() => LoadDataForResource(resource, serializer));
            await await Concurrency.TaskHelpers.RunOnMainThread(() => serializer.Deserialize(resource.Resource, data));

            resource.State = ResourceLoadingState.Loaded;

            if (!string.IsNullOrWhiteSpace(resource.Parameters))
                Log.WriteLine("Loaded {0}?{2} of type {1}", resource.Name, resource.Resource.GetType(), resource.Parameters);
            else
                Log.WriteLine("Loaded {0} of type {1}", resource.Name, resource.Resource.GetType());
        }

        private async Task<byte[]> LoadDataForResource(ResourceReference resource, IResourceSerializer serializer)
        {
            if (serializer.SupportsStreaming)
            {
                return null;
            }

            var path = resource.Name + serializer.Extension;

            if (!_fileSystem.FileExists(path) && !string.IsNullOrWhiteSpace(serializer.DefaultFilename))
            {
                path = serializer.DefaultFilename;
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
            var resourceType = resourceReference.Resource.GetType();

            IResourceSerializer serializer = DefaultResourceSerializer;
            if (_resourceSerializers.ContainsKey(resourceType))
                serializer = _resourceSerializers[resourceType];

            if (resourceReference.State == ResourceLoadingState.Loaded)
            {
                resourceReference.State = ResourceLoadingState.Unloading;

                void unloadAction()
                {
                    if (resourceReference.Resource is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (serializer is GenericResourceSerializer genericSerializer)
                    {
                        // Oh god no!
                        // Maybe it was better to have unload instead of dispose pattern
                        // Would at least make this part quite a bit cleaner
                        // Could implement a callback / new interface pattern to manage it as well
                        // Would allow a custom class for unloading which can be nice for some cases and IDisposable pattern for the rest
                        genericSerializer.Unload(resourceReference.Resource);
                    }

                    resourceReference.State = ResourceLoadingState.Unloaded;
                    Log.WriteLine("Unloaded {0} of type {1}", resourceReference.Name, resourceType);
                }

                var task = new Task(unloadAction);
                if (async)
                    Concurrency.TaskHelpers.RunOnMainThread(() => task.RunSynchronously());
                else
                    task.RunSynchronously();
            }
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
                resourceReference.RemoveReference();
        }

        public bool IsManaged(object resource)
            => _instanceToReference.ContainsKey(resource);

        public void AddResourceSerializer<TResource>(IResourceSerializer<TResource> serializer) where TResource : class
        {
            _resourceSerializers.Add(typeof(TResource), serializer ?? throw new ArgumentNullException(nameof(serializer)));
            KnownResourceTypes.Add(typeof(TResource));
        }

        public void GargabgeCollect()
        {
            _loadingLock.Wait();

            foreach (var resource in _resources.Values)
            {
                if (resource.ReferenceCount <= 0)
                {
                    _resourcesToUnload.Add(resource);
                }
            }

            foreach (var resource in _resourcesToUnload)
            {
                UnloadResource(resource);
                _resources.TryRemove(resource.Name + "?" + resource.Parameters, out var _);
                _instanceToReference.TryRemove(resource.Resource, out var _);
            }

            _resourcesToUnload.Clear();

            _loadingLock.Release();
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
