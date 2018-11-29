using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Globalization;
using Triton.Utility;

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
							Quaternion orientation = Quaternion.Identity;

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

									orientation = Quaternion.FromAxisAngle(axis, angle);
								}
							}

							skeleton.Bones.Insert(index, new Transform
							{
								Position = position,
								Orientation = orientation
							});
						}
					}
					// Read hierarchy
					else if (reader.Name == "bonehierarchy")
					{
						var boneReader = reader.ReadSubtree();

						while (boneReader.Read())
						{
							if (boneReader.NodeType != XmlNodeType.Element || boneReader.Name != "boneparent")
								continue;

							var index = nameToId[boneReader.GetAttribute("bone")];
							var parentId = nameToId[boneReader.GetAttribute("parent")];

							while (index >= skeleton.BoneParents.Count)
								skeleton.BoneParents.Add(-1);

							skeleton.BoneParents.Insert(index, parentId);
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

									var position = Vector3.Zero;
									var orientation = Quaternion.Identity;

									var keyframeReader = trackReader.ReadSubtree();
									while (keyframeReader.Read())
									{
										if (keyframeReader.NodeType != XmlNodeType.Element)
											continue;

										if (keyframeReader.Name == "translate")
										{
											position = ReadVector3(keyframeReader);
										}
										else if (keyframeReader.Name == "rotate")
										{
											var angle = StringConverter.Parse<float>(keyframeReader.GetAttribute("angle"));
											keyframeReader.ReadToFollowing("axis");
											var axis = ReadVector3(keyframeReader);

											orientation = Quaternion.FromAxisAngle(axis, angle);
										}
									}

									keyFrame.Transform.Position = position;
									keyFrame.Transform.Orientation = orientation;
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
