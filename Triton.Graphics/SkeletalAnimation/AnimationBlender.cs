using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	public class AniamtionBlender
	{
		private AnimationState _sourceState;
		private AnimationState _targetState;
		private float _timeLeft = 0;
		private float _duration;

		public AniamtionBlender()
		{
		}

		public void Blend(AnimationState source, AnimationState target, float duration = 1)
		{
			if (_sourceState == null)
			{
				_sourceState = target;
				_sourceState.Enabled = true;
				_sourceState.Weight = 1.0f;
				_sourceState.TimePosition = 0.0f;

				_timeLeft = 0.0f;
			}
			else if (_timeLeft > 0.0f)
			{
				if (_targetState == target)
				{
					// Nothing to see here, move along
				}
				else
				{
					if (_timeLeft < _duration * 0.5f)
					{
						_targetState.Enabled = false;
						_targetState.Weight = 0.0f;
						_targetState.TimePosition = 0.0f;
					}
					else
					{
						_sourceState.Enabled = false;
						_sourceState.Weight = 0.0f;
						_sourceState.TimePosition = 0.0f;
						_sourceState = _targetState;
					}

					_targetState = target;
					_targetState.Enabled = true;
					_targetState.Weight = 1.0f - (_timeLeft / _duration);
					_targetState.TimePosition = 0.0f;
				}
			}
			else
			{
				_timeLeft = _duration = duration;

				_targetState = target;
				_targetState.Weight = 0.0f;
				_targetState.TimePosition = 0.0f;
				_targetState.Enabled = true;

				if (_targetState == _sourceState)
				{
					_targetState.Weight = 1.0f;
					_timeLeft = 0.0f;
				}
			}
		}

		public void Update(float elapsedTime)
		{
			if (_sourceState == null)
				return;

			if (_timeLeft > 0.0f)
			{
				_timeLeft -= elapsedTime;

				if (_timeLeft <= 0.0f)
				{
					_sourceState.Enabled = false;
					_sourceState.Weight = 0.0f;

					_sourceState = _targetState;
					_sourceState.Enabled = true;
					_sourceState.Weight = 1.0f;

					_targetState = null;
				}
				else
				{
					float alpha = _timeLeft / _duration;

					_sourceState.Weight = alpha;
					_targetState.Weight = 1.0f - alpha;

					_targetState.AddTime(elapsedTime);
				}
			}

			_sourceState.AddTime(elapsedTime);
		}
	}
}
