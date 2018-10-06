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
		private readonly SharpFileSystem.IFileSystem _fileSystemImpl;

		public FileSystem(SharpFileSystem.IFileSystem fileSystem)
		{
            _fileSystemImpl = fileSystem ?? throw new ArgumentNullException("fileSystem");
        }

		private SharpFileSystem.FileSystemPath GetPath(string path)
		{
			return SharpFileSystem.FileSystemPath.Parse(path);
		}

		public bool FileExists(string filename)
		{
			return _fileSystemImpl.Exists(GetPath(filename));
		}

		public bool DirectoryExists(string filename)
		{
			return _fileSystemImpl.Exists(GetPath(filename));
		}

		public Stream OpenRead(string filename)
		{
			return _fileSystemImpl.OpenFile(GetPath(filename), FileAccess.Read);
		}

		public Stream OpenWrite(string filename)
		{
			var path = GetPath(filename);
			if (!_fileSystemImpl.Exists(path))
				return _fileSystemImpl.CreateFile(path);
			else
				return _fileSystemImpl.OpenFile(path, FileAccess.Write);
		}
	}
}
