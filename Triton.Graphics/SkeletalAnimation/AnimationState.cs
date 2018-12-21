using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.SkeletalAnimation
{
	public class AnimationState
	{
		public Animation Animation;

		public bool Enabled = false;
		public float TimePosition = 0;
		public float Weight = 1;
		public bool Loop = true;

		public string Name
		{
			get
			{
				return Animation.Name;
			}
		}

		public bool HasEnded
		{
			get
			{
				return !Loop && TimePosition >= Animation.Length;
			}
		}

		internal AnimationState(Animation animation)
		{
			Animation = animation;
		}

		public void AddTime(float time)
		{
			TimePosition += time;

			if (TimePosition > Animation.Length)
			{
				if (Loop)
					TimePosition = TimePosition - Animation.Length;
				else
					TimePosition = Animation.Length;
			}
		}
	}
}
