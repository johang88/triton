using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	public class ResourceGroupManager
	{
		private readonly List<ResourceGroup> Groups = new List<ResourceGroup>();
		private readonly IO.FileSystem FileSystem;

		public ResourceGroupManager(IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystem = fileSystem;
		}

		public ResourceManager Add(string name, int maxResourcesPerFrame = 10)
		{
			if (Groups.Exists(r => r.Name == name))
				throw new ArgumentException(string.Format("resource group '{0}' already exists", name));

			var group = new ResourceGroup
			{
				Name = name,
				ResourceManager = new ResourceManager(FileSystem),
				MaxResourcesPerFrame = maxResourcesPerFrame
			};

			Groups.Add(group);

			return group.ResourceManager;
		}

		public ResourceManager Get(string name)
		{
			foreach (var group in Groups)
			{
				if (group.Name == name)
					return group.ResourceManager;
			}

			throw new KeyNotFoundException(name);
		}

		public void TickResourceLoading()
		{
			foreach (var group in Groups)
			{
				group.ResourceManager.TickResourceLoading(group.MaxResourcesPerFrame);
			}
		}

		class ResourceGroup
		{
			public string Name;
			public ResourceManager ResourceManager;
			public int MaxResourcesPerFrame;
		}
	}
}
