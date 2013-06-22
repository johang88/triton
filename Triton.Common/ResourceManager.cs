﻿using System;
using System.Collections.Generic;
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
		private readonly Dictionary<string, Resource> Resources = new Dictionary<string, Resource>();
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

			lock (LoadingLock)
			{
				var loader = ResourceLoaders[typeof(TResource)];

				Resource resource = null;
				if (!Resources.TryGetValue(name, out resource))
				{
					resource = loader.Create(name, parameters);
					Resources.Add(name, resource);
				}

				resource.ReferenceCount += 1;

				if (resource.State == ResourceLoadingState.Unloaded)
				{
					resource.State = ResourceLoadingState.Loading;

					AddItemToWorkQueue(() =>
					{
						loader.Load(resource, parameters, (r) =>
						{
							resource.State = ResourceLoadingState.Loaded;

							Log.WriteLine("Loaded {0}", name);

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
			if (!ResourceLoaders.ContainsKey(resource.GetType()))
				throw new InvalidOperationException("no resource loader for the specified type");

			lock (LoadingLock)
			{
				if (resource.State == ResourceLoadingState.Loaded)
				{
					resource.State = ResourceLoadingState.Unloading;
					var loader = ResourceLoaders[resource.GetType()];

					Action unloadAction = () =>
					{
						loader.Unload(resource);
						resource.State = ResourceLoadingState.Unloaded;
						Log.WriteLine("Unloaded {0}", resource.Name);
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
				Resources.Add(resource.Name, resource);
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

		public bool AllResourcesLoaded()
		{
			return Resources.All(r => r.Value.State == ResourceLoadingState.Loaded);
		}
	}
}
