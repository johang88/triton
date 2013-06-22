using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Common.IO
{
	public delegate void FileChanged(string path);

	/// <summary>
	/// Abstract access to the file system by giving the ability to mount packages.
	/// These packages can be locations in the location file system, zip files or even remote locations.
	/// </summary>
	public class FileSystem
	{
		private readonly List<KeyValuePair<PackagePriority, IPackage>> Packages = new List<KeyValuePair<PackagePriority, IPackage>>();
		private readonly Dictionary<string, PackageCreator> PackageFactory = new Dictionary<string, PackageCreator>();

		public delegate IPackage PackageCreator(string path);

		private readonly List<FileChanged> FileChangedListeners = new List<FileChanged>();

		public FileSystem()
		{
			RegisterPackageType("FileSystem", p => new FileSystemPackage(p, this));
		}

		public void AddFileChangedListener(FileChanged callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback");

			FileChangedListeners.Add(callback);
		}

		internal void OnFileChanged(string path)
		{
			System.Threading.Thread.Sleep(1000);
			FileChangedListeners.ForEach(l => l(path));
		}

		public bool FileExists(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
				throw new ArgumentException("filename is null or empty");

			filename = ToSystemPath(filename);

			foreach (KeyValuePair<PackagePriority, IPackage> package in Packages)
			{
				if (package.Value.FileExists(filename))
				{
					return true;
				}
			}

			return false;
		}

		public Stream OpenRead(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
				throw new ArgumentException("filename is null or empty");

			filename = ToSystemPath(filename);

			// Open the file using the first package where a file with the given filename exists
			foreach (KeyValuePair<PackagePriority, IPackage> package in Packages)
			{
				if (package.Value.FileExists(filename))
				{
					return package.Value.OpenFile(filename);
				}
			}

			throw new FileNotFoundException(filename);
		}

		public Stream OpenWrite(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
				throw new ArgumentException("filename is null or empty");

			filename = ToSystemPath(filename);

			foreach (KeyValuePair<PackagePriority, IPackage> package in Packages)
			{
				if (!package.Value.Writeable)
					continue;

				return package.Value.OpenWrite(filename);
			}

			throw new FileNotFoundException(filename);
		}

		public IEnumerable<string> GetDirectories(string path)
		{
			path = ToSystemPath(path);
			HashSet<string> entries = new HashSet<string>();

			foreach (KeyValuePair<PackagePriority, IPackage> package in Packages)
			{
				foreach (string directoryName in package.Value.GetDirectories(path))
				{
					entries.Add(FromSystemPath(directoryName));
				}
			}

			return entries;
		}

		public IEnumerable<string> GetFiles(string path)
		{
			return GetFiles(path, "*");
		}

		public IEnumerable<string> GetFiles(string path, string pattern)
		{
			if (pattern == null)
				pattern = "*";

			HashSet<string> entries = new HashSet<string>();

			foreach (KeyValuePair<PackagePriority, IPackage> package in Packages)
			{
				foreach (string filename in package.Value.GetFiles(path, pattern))
				{
					entries.Add(ToSystemPath(filename));
				}
			}

			return entries;
		}

		public void AddPackage(IPackage package)
		{
			AddPackage(package, PackagePriority.Medium);
		}

		public void AddPackage(IPackage package, PackagePriority priority)
		{
			if (package == null)
				throw new ArgumentNullException("package");

			Packages.Add(new KeyValuePair<PackagePriority, IPackage>(priority, package));

			Packages.Sort((d1, d2) => d1.Key == d2.Key ? 0 : d1.Key > d2.Key ? -1 : 1);
		}

		public void AddPackage(string typeName, string path)
		{
			AddPackage(PackageFactory[typeName](path));

			Log.WriteLine(string.Format("Package '{0}' of type {1} added", path, typeName));
		}

		public void AddPackage(string typeName, string path, PackagePriority priority)
		{
			AddPackage(PackageFactory[typeName](path), priority);

			Log.WriteLine(string.Format("Package '{0}' of type {1} added", path, typeName));
		}

		public void RegisterPackageType(string name, PackageCreator creator)
		{
			if (creator == null)
				throw new ArgumentNullException("creator");

			Log.WriteLine(string.Format("Package factory for  type {0} registered", name));

			PackageFactory.Add(name, creator);
		}

		internal static string FromSystemPath(string path)
		{
			return path.Replace(Path.DirectorySeparatorChar, '/');
		}

		internal static string ToSystemPath(string path)
		{
			path = path.Replace("..", "").Replace('/', Path.DirectorySeparatorChar);

			while (path.StartsWith("." + Path.DirectorySeparatorChar))
			{
				path = path.Substring(("." + Path.DirectorySeparatorChar).Length);
			}

			while (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
			{
				path = path.Substring(1);
			}

			return path;
		}
	}
}
