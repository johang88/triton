using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using Triton.Common;

namespace Triton.Content.Skeletons.Converters
{
	class OgreXmlConverter : ISkeletonConverter
	{
		public Skeleton Import(Stream stream)
		{
			using (var reader = XmlReader.Create(stream))
			{
				var skeleton = new Skeleton();
				Dictionary<string, int> nameToId = new Dictionary<string, int>();

				while (reader.Read())
				{
					if (reader.NodeType != XmlNodeType.Element)
						continue;

					// Read bones
					if (reader.Name == "bones")
					{
						var bonesReader = reader.ReadSubtree();

						while (bonesReader.Read())
						{
							if (bonesReader.NodeType != XmlNodeType.Element || bonesReader.Name != "bone")
								continue;

							var index = StringConverter.Parse<int>(bonesReader.GetAttribute("id"));
							var name = bonesReader.GetAttribute("name");

							nameToId.Add(name, index);

							Vector3 position = Vector3.Zero;
							Matrix4 transform = Matrix4.Identity;

							var boneReader = bonesReader.ReadSubtree();
							while (boneReader.Read())
							{
								if (boneReader.NodeType != XmlNodeType.Element)
									continue;

								if (boneReader.Name == "position")
								{
									position = ReadVector3(boneReader);
								}
								else if (boneReader.Name == "rotation")
								{
									var angle = StringConverter.Parse<float>(boneReader.GetAttribute("angle"));
									boneReader.ReadToFollowing("axis");
									var axis = ReadVector3(boneReader);

									transform = Matrix4.CreateFromAxisAngle(axis, angle);
								}
							}

							transform = transform * Matrix4.CreateTranslation(position);

							skeleton.Bones.Insert(index, transform);
						}
					}
					// Read animations
					else if (reader.Name == "animations")
					{
						var animationsReader = reader.ReadSubtree();

						while (animationsReader.Read())
						{
							if (animationsReader.NodeType != XmlNodeType.Element || animationsReader.Name != "animation")
								continue;

							var animation = new Animation();
							skeleton.Animations.Add(animation);

							animation.Name = animationsReader.GetAttribute("name");
							animation.Length = StringConverter.Parse<float>(animationsReader.GetAttribute("length"));

							var animationReader = animationsReader.ReadSubtree();

							while (animationReader.Read())
							{
								if (animationReader.NodeType != XmlNodeType.Element || animationReader.Name != "track")
									continue;

								var track = new Track();
								animation.Tracks.Add(track);

								track.BoneIndex = nameToId[animationReader.GetAttribute("bone")];

								var trackReader = animationReader.ReadSubtree();
								while (trackReader.Read())
								{
									if (trackReader.NodeType != XmlNodeType.Element || trackReader.Name != "keyframe")
										continue;

									var keyFrame = new KeyFrame();
									track.KeyFrames.Add(keyFrame);

									keyFrame.Time = StringConverter.Parse<float>(trackReader.GetAttribute("time"));

									Vector3 position = Vector3.Zero;
									Matrix4 transform = Matrix4.Identity;

									var keyframeReader = trackReader.ReadSubtree();
									while (keyframeReader.Read())
									{
										if (keyframeReader.Name == "position")
										{
											position = ReadVector3(keyframeReader);
										}
										else if (keyframeReader.Name == "rotation")
										{
											var angle = StringConverter.Parse<float>(keyframeReader.GetAttribute("angle"));
											keyframeReader.ReadToFollowing("axis");
											var axis = ReadVector3(keyframeReader);

											transform = Matrix4.CreateFromAxisAngle(axis, angle);
										}
									}

									transform = transform * Matrix4.CreateTranslation(position);
									keyFrame.Transform = transform;
								}
							}
						}
					}
				}

				return skeleton;
			}
		}

		float ParseFloat(string value)
		{
			return StringConverter.Parse<float>(value);
		}

		Triton.Vector3 ReadVector3(XmlReader reader)
		{
			return new Triton.Vector3(ParseFloat(reader.GetAttribute("x")), ParseFloat(reader.GetAttribute("y")), ParseFloat(reader.GetAttribute("z")));
		}

		Triton.Vector2 ReadUV(XmlReader reader)
		{
			return new Triton.Vector2(ParseFloat(reader.GetAttribute("u")), ParseFloat(reader.GetAttribute("v")));
		}
	}
}
