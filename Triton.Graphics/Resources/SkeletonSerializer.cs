using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics.SkeletalAnimation;
using Triton.IO;

namespace Triton.Graphics.Resources
{
	class SkeletonSerializer : Triton.Resources.IResourceSerializer<Skeleton>
	{
		static readonly char[] Magic = new char[] { 'S', 'K', 'E', 'L' };
		const int Version_1 = 0x0100;

		private readonly Triton.IO.FileSystem FileSystem;

        public bool SupportsStreaming => false;

        public SkeletonSerializer(Triton.IO.FileSystem fileSystem)
		{
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

		public string Extension { get { return ".skeleton"; } }
		public string DefaultFilename { get { return ""; } }

		public object Create(Type type)
		=> new Skeleton();

		public Task Deserialize(object resource, byte[] data)
		{
			var skeleton = (Skeleton)resource;

			using (var stream = new System.IO.MemoryStream(data))
			using (var reader = new System.IO.BinaryReader(stream))
			{
				var magic = reader.ReadChars(4);
				for (var i = 0; i < Magic.Length; i++)
				{
					if (magic[i] != Magic[i])
						throw new ArgumentException("invalid skeleton");
				}

				var version = reader.ReadInt32();

				var validVersions = new int[] { Version_1 };

				if (!validVersions.Contains(version))
					throw new ArgumentException("invalid skeleton, unknown version");

				var boneCount = reader.ReadInt32();
				skeleton.BindPose = new Transform[boneCount];

				for (var i = 0; i < boneCount; i++)
				{
					skeleton.BindPose[i].Position = reader.ReadVector3();
					skeleton.BindPose[i].Orientation = reader.ReadQuaternion();
				}

				var boneParentCount = reader.ReadInt32();
				skeleton.BoneParents = new int[boneParentCount];

				for (var i = 0; i < boneParentCount; i++)
				{
					skeleton.BoneParents[i] = reader.ReadInt32();
				}

				var animationCount = reader.ReadInt32();
				skeleton.Animations = new Animation[animationCount];

				for (var i = 0; i < animationCount; i++)
				{
					var animation = new Animation(reader.ReadString(), reader.ReadSingle());

					var trackCount = reader.ReadInt32();
					animation.Tracks = new Track[trackCount];

					for (var t = 0; t < trackCount; t++)
					{
						animation.Tracks[t].BoneIndex = reader.ReadInt32();

						var keyFrameCount = reader.ReadInt32();
						animation.Tracks[t].KeyFrames = new KeyFrame[keyFrameCount];

						for (var k = 0; k < keyFrameCount; k++)
						{
							animation.Tracks[t].KeyFrames[k].Time = reader.ReadSingle();
							animation.Tracks[t].KeyFrames[k].Transform.Position = reader.ReadVector3();
							animation.Tracks[t].KeyFrames[k].Transform.Orientation = reader.ReadQuaternion();
						}
					}

					skeleton.Animations[i] = animation;
				}
			}

            return Task.FromResult(0);
        }

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }
}
