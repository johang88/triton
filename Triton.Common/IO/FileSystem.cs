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
		private readonly SharpFileSystem.IFileSystem FileSystemImpl;

		public FileSystem(SharpFileSystem.IFileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			FileSystemImpl = fileSystem;
		}

		private SharpFileSystem.FileSystemPath GetPath(string path)
		{
			if (!path.StartsWith("/"))
			{
				Log.WriteLine(string.Format("path is not rooted: {0}", path), LogLevel.Debug);
				path = '/' + path;
			}

			return SharpFileSystem.FileSystemPath.Parse(path);
		}

		public bool FileExists(string filename)
		{
			return FileSystemImpl.Exists(GetPath(filename));
		}

		public Stream OpenRead(string filename)
		{
			return FileSystemImpl.OpenFile(GetPath(filename), FileAccess.Read);
		}

		public Stream OpenWrite(string filename)
		{
			return FileSystemImpl.OpenFile(GetPath(filename), FileAccess.Write);
		}
	}
}
