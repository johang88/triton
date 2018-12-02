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

		public Matrix4[] FinalBoneTransforms;

		private Transform[] _boneTransforms;
		private Transform[] _worldTransforms;
		private Transform[] _inverseTransforms;

		private readonly List<AnimationState> _animationStates = new List<AnimationState>();

		public SkeletonInstance(Skeleton skeleton)
		{
			Skeleton = skeleton;
			Init();
		}

		public SkeletonInstance(Resources.Mesh mesh)
		{
		    Skeleton = mesh.Skeleton ?? throw new ArgumentNullException(nameof(mesh.Skeleton));

			Init();
		}

		private void Init()
		{
			_boneTransforms = Skeleton.GetInitialPose();
			_worldTransforms = Skeleton.GetInitialPose();
			_inverseTransforms = Skeleton.GetInitialPose();

			FinalBoneTransforms = new Matrix4[_boneTransforms.Length];

			// Apply parent transform
			for (var i = 1; i < _inverseTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];
			    if (parentIndex < 0) parentIndex = 0;
				_inverseTransforms[i].Orientation = _inverseTransforms[parentIndex].Orientation * _inverseTransforms[i].Orientation;
				_inverseTransforms[i].Position = Vector3.Transform(_inverseTransforms[i].Position, _inverseTransforms[parentIndex].Orientation) + _inverseTransforms[parentIndex].Position;
			}

			// Invert transform
			for (var i = 0; i < _inverseTransforms.Length; i++)
			{
				_inverseTransforms[i].Orientation = Quaternion.Invert(_inverseTransforms[i].Orientation);
				_inverseTransforms[i].Position = -_inverseTransforms[i].Position;
			}

			ResetBindPose();
		}

		private void ResetBindPose()
		{
			for (var i = 0; i < Skeleton.BindPose.Length; i++)
			{
				_boneTransforms[i] = Skeleton.BindPose[i];
			}
		}

		public AnimationState GetAnimationState(string name)
		{
			// Do a linear search
			foreach (var state in _animationStates)
			{
				if (state.Animation.Name == name)
					return state;
			}

			// Create the animation state
			foreach (var animation in Skeleton.Animations)
			{
				if (animation.Name == name)
				{
					var state = new AnimationState(animation);
					_animationStates.Add(state);
					return state;
				}
			}

			throw new ArgumentException("animation not found");
		}

		private void ApplyAnimation(Animation animation, float timePosition, float weight)
		{
			var noRotation = Quaternion.Identity;
			var noTranslation = Vector3.Zero;

			Quaternion rotation;
			Vector3 translation;

			for (var t = 0; t < animation.Tracks.Length; t++)
			{
				var index = animation.Tracks[t].BoneIndex;
				var keyFrameIndex = 0;

				// Find active key frame
				for (var k = 0; k < animation.Tracks[t].KeyFrames.Length; k++)
				{
					if (animation.Tracks[t].KeyFrames[k].Time > timePosition)
						break;

					keyFrameIndex = k;
				}

				var keyFrame = animation.Tracks[t].KeyFrames[keyFrameIndex];
				var transform = keyFrame.Transform;

				// Interpolate between key frames if neccecary
				if (keyFrameIndex < animation.Tracks[t].KeyFrames.Length - 1)
				{
					var keyFrame2 = animation.Tracks[t].KeyFrames[keyFrameIndex + 1];

					var alpha = timePosition - keyFrame.Time;
					if (alpha > 0)
					{
						alpha = alpha / (keyFrame2.Time - keyFrame.Time);

						Quaternion.Slerp(ref keyFrame.Transform.Orientation, ref keyFrame2.Transform.Orientation, alpha, out transform.Orientation);
						Vector3.Lerp(ref keyFrame.Transform.Position, ref keyFrame2.Transform.Position, alpha, out transform.Position);
					}
				}

				Quaternion.Slerp(ref noRotation, ref transform.Orientation, weight, out rotation);
				Vector3.Lerp(ref noTranslation, ref transform.Position, weight, out translation);

				Quaternion.Multiply(ref _boneTransforms[index].Orientation, ref rotation, out _boneTransforms[index].Orientation);
				Vector3.Add(ref _boneTransforms[index].Position, ref translation, out _boneTransforms[index].Position);
			}
		}

		public void Update()
		{
			for (var i = 0; i < _boneTransforms.Length; i++)
			{
				_boneTransforms[i].Position = Vector3.Zero;
				_boneTransforms[i].Orientation = Quaternion.Identity;
			}

			float weightFactor = 1.0f;
			float totalWeights = 0.0f;

			foreach (var state in _animationStates)
			{
				if (state.Enabled)
				{
					totalWeights += state.Weight;
				}
			}

			if (totalWeights > 1.0f)
			{
				weightFactor = 1.0f / totalWeights;
			}

			foreach (var state in _animationStates)
			{
				if (!state.Enabled)
					continue;

				ApplyAnimation(state.Animation, state.TimePosition, state.Weight * weightFactor);
			}

			for (var i = 0; i < _boneTransforms.Length; i++)
			{
				var transform = _boneTransforms[i];

				Quaternion.Multiply(ref Skeleton.BindPose[i].Orientation, ref transform.Orientation, out _boneTransforms[i].Orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref transform.Position, ref Skeleton.BindPose[i].Orientation, out transformedPosition);
				Vector3.Add(ref transformedPosition, ref Skeleton.BindPose[i].Position, out _boneTransforms[i].Position);
			}

			_worldTransforms[0] = _boneTransforms[0];

			// Apply parent transform
			for (var i = 1; i < _boneTransforms.Length; i++)
			{
				var parentIndex = Skeleton.BoneParents[i];

				Quaternion.Multiply(ref _worldTransforms[parentIndex].Orientation, ref _boneTransforms[i].Orientation, out _worldTransforms[i].Orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref _boneTransforms[i].Position, ref _worldTransforms[parentIndex].Orientation, out transformedPosition);
				Vector3.Add(ref transformedPosition, ref _worldTransforms[parentIndex].Position, out _worldTransforms[i].Position);
			}

			// Calculate final bone matrix
			for (var i = 0; i < _worldTransforms.Length; i++)
			{
				Quaternion orientation;
				Quaternion.Multiply(ref  _worldTransforms[i].Orientation, ref _inverseTransforms[i].Orientation, out orientation);

				Vector3 transformedPosition;
				Vector3.Transform(ref _inverseTransforms[i].Position, ref orientation, out transformedPosition);

				Vector3 position;
				Vector3.Add(ref transformedPosition, ref _worldTransforms[i].Position, out position);

				Matrix4 translationMatrix;
				Matrix4.CreateTranslation(ref position, out translationMatrix);

				Matrix4 rotationMatrix;
				Matrix4.Rotate(ref orientation, out rotationMatrix);

				Matrix4.Mult(ref rotationMatrix, ref translationMatrix, out FinalBoneTransforms[i]);
			}
		}
	}
}
