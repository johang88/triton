using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	public class SkeletonInstance
	{
		public Skeleton Skeleton { get; private set; }
		private readonly Resources.Mesh Mesh;

		public Matrix4[] FinalBoneTransforms;

		private Transform[] BoneTransforms;
		private Transform[] WorldTransforms;
		private Transform[] InverseTransforms;

		private float Time = 0.0f;
		private Animation CurrentAnimation = null;
		private string CurrentAnimationName = null;
		private bool Initialized = false;

		public SkeletonInstance(Skeleton skeleton)
		{
			Skeleton = skeleton;
		}

		public SkeletonInstance(Resources.Mesh mesh)
		{
			Mesh = mesh;
		}

		private void Init()
		{
			BoneTransforms = Skeleton.GetInitialPose();
			WorldTransforms = Skeleton.GetInitialPose();
			InverseTransforms = Skeleton.GetInitialPose();

			FinalBoneTransforms = new Matrix4[BoneTransforms.Length];

			// Apply parent transform
			for (var i = 1; i < InverseTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];
				InverseTransforms[i].Orientation = InverseTransforms[parentIndex].Orientation * InverseTransforms[i].Orientation;
				InverseTransforms[i].Position = Vector3.Transform(InverseTransforms[i].Position, InverseTransforms[parentIndex].Orientation) + InverseTransforms[parentIndex].Position;
			}

			// Invert transform
			for (var i = 0; i < InverseTransforms.Length; i++)
			{
				InverseTransforms[i].Orientation = Quaternion.Invert(InverseTransforms[i].Orientation);
				InverseTransforms[i].Position = -InverseTransforms[i].Position;
			}

			Initialized = true;

			if (!string.IsNullOrWhiteSpace(CurrentAnimationName))
			{
				Play(CurrentAnimationName);
			}
		}

		public void Play(string animation)
		{
			CurrentAnimationName = animation;

			if (!Initialized)
				return;

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
			if (!Initialized)
			{
				if (Mesh != null && Mesh.State == Common.ResourceLoadingState.Loaded && Skeleton == null)
				{
					Skeleton = Mesh.Skeleton;
				}

				if (Skeleton != null && Skeleton.State == Common.ResourceLoadingState.Loaded)
				{
					Init();
				}
				else
				{
					return;
				}
			}

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

				// Interpolate between key frames if neccecary
				if (keyFrameIndex < CurrentAnimation.Tracks[t].KeyFrames.Length - 1)
				{
					var keyFrame2 = CurrentAnimation.Tracks[t].KeyFrames[keyFrameIndex + 1];

					var alpha = Time - keyFrame.Time;
					if (alpha > 0)
					{
						alpha = alpha / (keyFrame2.Time - keyFrame.Time);

						Quaternion.Slerp(ref keyFrame.Transform.Orientation, ref keyFrame2.Transform.Orientation, alpha, out transform.Orientation);
						Vector3.Lerp(ref keyFrame.Transform.Position, ref keyFrame2.Transform.Position, alpha, out transform.Position);
					}
				}

				Quaternion.Multiply(ref Skeleton.BindPose[index].Orientation, ref transform.Orientation, out BoneTransforms[index].Orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref transform.Position, ref Skeleton.BindPose[index].Orientation, out transformedPosition);
				Vector3.Add(ref transformedPosition, ref Skeleton.BindPose[index].Position, out BoneTransforms[index].Position);
			}

			WorldTransforms[0] = BoneTransforms[0];

			// Apply parent transform
			for (var i = 1; i < BoneTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];

				Quaternion.Multiply(ref WorldTransforms[parentIndex].Orientation, ref BoneTransforms[i].Orientation, out WorldTransforms[i].Orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref BoneTransforms[i].Position, ref WorldTransforms[parentIndex].Orientation, out transformedPosition);
				Vector3.Add(ref transformedPosition, ref WorldTransforms[parentIndex].Position, out WorldTransforms[i].Position);
			}

			// Calculate final bone matrix
			for (var i = 0; i < WorldTransforms.Length; i++)
			{
				Quaternion orientation;
				Quaternion.Multiply(ref  WorldTransforms[i].Orientation, ref InverseTransforms[i].Orientation, out orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref InverseTransforms[i].Position, ref orientation, out transformedPosition);

				Vector3 position;
				Vector3.Add(ref transformedPosition, ref WorldTransforms[i].Position, out position);

				Matrix4 translationMatrix;
				Matrix4.CreateTranslation(ref position, out translationMatrix);

				Matrix4 rotationMatrix;
				Matrix4.Rotate(ref orientation, out rotationMatrix);

				Matrix4.Mult(ref rotationMatrix, ref translationMatrix, out FinalBoneTransforms[i]);
			}
		}
	}
}
