using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFileSystem.FileSystems;
using SharpFileSystem;

namespace Triton.Samples
{
	public abstract class BaseGame : Triton.Game.Game
	{
		public BaseGame(string name)
			: base(name)
		{
			Width = 1280;
			Height = 720;
		}

		protected KeyValuePair<FileSystemPath, IFileSystem> CreateMount(string mountPoint, IFileSystem fileSystem)
		{
			return new KeyValuePair<FileSystemPath, IFileSystem>(FileSystemPath.Parse(mountPoint), fileSystem);
		}

		protected override SharpFileSystem.IFileSystem MountFileSystem()
		{
			return new MergedFileSystem(
				new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/core_data/")),
				new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/samples_data/")),
				// /tmp mount
				new FileSystemMounter(CreateMount("/tmp/", new PhysicalFileSystem("./tmp")))
				);
		}
	}
}
