using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	public class SkeletonInstance
	{
		public readonly Skeleton Skeleton;
		public Matrix4[] FinalBoneTransforms;

		private Transform[] BoneTransforms;
		private Transform[] WorldTransforms;
		private Transform[] InverseTransforms;

		private float Time = 0.0f;
		private Animation CurrentAnimation = null;

		public SkeletonInstance(Skeleton skeleton)
		{
			Skeleton = skeleton;

			BoneTransforms = skeleton.GetInitialPose();
			WorldTransforms = skeleton.GetInitialPose();
			InverseTransforms = skeleton.GetInitialPose();

			FinalBoneTransforms = new Matrix4[BoneTransforms.Length];

			// Apply parent transform
			for (var i = 1; i < InverseTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];
				if (parentIndex == -1)
					continue;

				InverseTransforms[i].Orientation = InverseTransforms[parentIndex].Orientation * InverseTransforms[i].Orientation;
				InverseTransforms[i].Position = Vector3.Transform(InverseTransforms[i].Position, InverseTransforms[parentIndex].Orientation) + InverseTransforms[parentIndex].Position;
			}

			// Invert transform
			for (var i = 0; i < InverseTransforms.Length; i++)
			{
				InverseTransforms[i].Orientation = Quaternion.Invert(InverseTransforms[i].Orientation);
				InverseTransforms[i].Position = -InverseTransforms[i].Position;
			}
		}

		public void Play(string animation)
		{
			CurrentAnimation = Skeleton.Animations.FirstOrDefault(a => a.Name == animation);
			Time = 0.0f;

			if (CurrentAnimation == null)
				return;

			// Reset bind pose
			for (var i = 0; i < Skeleton.BindPose.Length; i++)
			{
				BoneTransforms[i] = Skeleton.BindPose[i];
			}
		}

		public void Update(float stepSize)
		{
			if (CurrentAnimation == null)
				return;

			Time += stepSize;
			if (Time >= CurrentAnimation.Length)
			{
				Time = 0.0f;

				// Reset bind pose
				for (var i = 0; i < Skeleton.BindPose.Length; i++)
				{
					BoneTransforms[i] = Skeleton.BindPose[i];
				}
			}

			// Apply animation track transform
			for (var t = 0; t < CurrentAnimation.Tracks.Length; t++)
			{
				var index = CurrentAnimation.Tracks[t].BoneIndex;
				var keyFrameIndex = 0;

				// Find active key frame
				for (var k = 0; k < CurrentAnimation.Tracks[t].KeyFrames.Length; k++)
				{
					if (CurrentAnimation.Tracks[t].KeyFrames[k].Time > Time)
						break;

					keyFrameIndex = k;
				}

				var keyFrame = CurrentAnimation.Tracks[t].KeyFrames[keyFrameIndex];
				var transform = keyFrame.Transform;

				if (keyFrameIndex < CurrentAnimation.Tracks[t].KeyFrames.Length - 1)
				{
					var keyFrame2 = CurrentAnimation.Tracks[t].KeyFrames[keyFrameIndex + 1];

					var alpha = Time - keyFrame.Time;
					if (alpha > 0)
					{
						alpha = alpha / (keyFrame2.Time - keyFrame.Time);

						transform.Orientation = Quaternion.Slerp(keyFrame.Transform.Orientation, keyFrame2.Transform.Orientation, alpha);
						transform.Position = Vector3.Lerp(keyFrame.Transform.Position, keyFrame2.Transform.Position, alpha);
					}
				}

				BoneTransforms[index].Orientation = Skeleton.BindPose[index].Orientation * transform.Orientation;
				BoneTransforms[index].Position = Vector3.Transform(transform.Position, Skeleton.BindPose[index].Orientation) + Skeleton.BindPose[index].Position;
			}

			WorldTransforms[0] = BoneTransforms[0];

			// Apply parent transform
			for (var i = 1; i < BoneTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];
				WorldTransforms[i].Orientation = WorldTransforms[parentIndex].Orientation * BoneTransforms[i].Orientation;
				WorldTransforms[i].Position = Vector3.Transform(BoneTransforms[i].Position, WorldTransforms[parentIndex].Orientation) + WorldTransforms[parentIndex].Position;
			}

			// Calculate final bone matrix
			for (var i = 0; i < WorldTransforms.Length; i++)
			{
				Quaternion orientation = WorldTransforms[i].Orientation * InverseTransforms[i].Orientation;
				Vector3 position = WorldTransforms[i].Position + Vector3.Transform(InverseTransforms[i].Position, orientation);

				Matrix4 translationMatrix;
				Matrix4.CreateTranslation(ref position, out translationMatrix);

				Matrix4 rotationMatrix = Matrix4.Rotate(orientation);

				Matrix4.Mult(ref rotationMatrix, ref translationMatrix, out FinalBoneTransforms[i]);
			}
		}
	}
}
