using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using Triton.Content.Skeletons;
using Triton.Tools;
using Triton.IO;

namespace Triton.Content.Compilers
{
	public class SkeletonCompiler : ICompiler
	{
		private Factory<string, ISkeletonConverter> ImporterFactory;

		const int Version = 0x0100;

		public SkeletonCompiler()
		{
			ImporterFactory = new Factory<string, ISkeletonConverter>();
			ImporterFactory.Add(".xml", () => new Skeletons.Converters.OgreXmlConverter());
		}

		public void Compile(CompilationContext context, string inputPath, string outputPath, Database.ContentEntry contentData)
        {
            outputPath += ".skeleton";

            string extension = Path.GetExtension(inputPath.Replace(".skeleton.xml", ".xml")).ToLowerInvariant();

            var importer = ImporterFactory.Create(extension);
            var skeleton = importer.Import(File.OpenRead(inputPath));

            SerializeSkeleton(outputPath, skeleton);
        }

        internal static void SerializeSkeleton(string outputPath, Skeleton skeleton)
        {
            using (var stream = File.Open(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // Magic
                writer.Write('S');
                writer.Write('K');
                writer.Write('E');
                writer.Write('L');

                // Version
                writer.Write(Version);

                // Bone count
                writer.Write(skeleton.Bones.Count);

                // Bones
                foreach (var bone in skeleton.Bones)
                {
                    writer.Write(bone.Position);
                    writer.Write(bone.Orientation);
                }

                // Parent count
                writer.Write(skeleton.BoneParents.Count);
                foreach (var parent in skeleton.BoneParents)
                {
                    writer.Write(parent);
                }

                // Animation count
                writer.Write(skeleton.Animations.Count);

                // Animations
                foreach (var animation in skeleton.Animations)
                {
                    writer.Write(animation.Name);
                    writer.Write(animation.Length);

                    // Track count
                    writer.Write(animation.Tracks.Count);

                    // Tracks
                    foreach (var track in animation.Tracks)
                    {
                        writer.Write(track.BoneIndex);

                        // Key frame count
                        writer.Write(track.KeyFrames.Count);

                        // Key frames
                        foreach (var keyFrame in track.KeyFrames)
                        {
                            writer.Write(keyFrame.Time);
                            writer.Write(keyFrame.Transform.Position);
                            writer.Write(keyFrame.Transform.Orientation);
                        }
                    }
                }
            }
        }
    }
}
