using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	class AudioSource : IAudioSource
	{
		private bool IsDisposed;
		internal IAudioBuffer Buffer { get; private set; }
		private bool _Relative;
		private bool _Loop;
		private Vector3 _Position;
		private Vector3 _Velocity;
		private float _Range;
		private float _Pitch;
		private float _Gain;
		private float _RolloffFactor;
		private int Source;

		public AudioSource(IAudioBuffer buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			Source = AL.GenSource();
			Util.CheckOpenAlErrors();

			IsDisposed = false;

			Buffer = buffer;
			Buffer.Bind(Source);

			Loop = false;
			Position = Vector3.Zero;
			Velocity = Vector3.Zero;
		}

		~AudioSource()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				Stop();
				AL.DeleteSource(Source);
				Util.CheckOpenAlErrors();
			}
		}

		internal void Update()
		{
			Buffer.Update(Source);
		}

		public void Play()
		{
			Buffer.Update(Source);
			AL.SourcePlay(Source);
			Util.CheckOpenAlErrors();
		}

		public void Stop()
		{
			AL.SourceStop(Source);
			Util.CheckOpenAlErrors();
		}

		public void Pause()
		{
			AL.SourcePause(Source);
			Util.CheckOpenAlErrors();
		}

		public void Rewind()
		{
			AL.SourceRewind(Source);
			Util.CheckOpenAlErrors();
		}

		public bool Relative
		{
			get
			{
				return _Relative;
			}
			set
			{
				_Relative = value;
				AL.Source(Source, ALSourceb.SourceRelative, _Relative);
				Util.CheckOpenAlErrors();
			}
		}

		public bool IsPlaying
		{
			get
			{
				var state = AL.GetSourceState(Source);
				return state == ALSourceState.Playing;
			}
		}

		public bool Loop
		{
			get
			{
				return _Loop;
			}
			set
			{
				_Loop = value;
				AL.Source(Source, ALSourceb.Looping, _Loop);
				Util.CheckOpenAlErrors();
			}
		}

		public Vector3 Position
		{
			get
			{
				return _Position;
			}
			set
			{
				_Position = value;
				AL.Source(Source, ALSource3f.Position, _Position.X, _Position.Y, _Position.Z);
				Util.CheckOpenAlErrors();
			}
		}

		public Vector3 Velocity
		{
			get
			{
				return _Velocity;
			}
			set
			{
				_Velocity = value;
				AL.Source(Source, ALSource3f.Velocity, _Velocity.X, _Velocity.Y, _Velocity.Z);
				Util.CheckOpenAlErrors();
			}
		}

		public float Range
		{
			get
			{
				return _Range;
			}
			set
			{
				_Range = value;
				AL.Source(Source, ALSourcef.MaxDistance, _Range);
				Util.CheckOpenAlErrors();
			}
		}

		public float Pitch
		{
			get
			{
				return _Pitch;
			}
			set
			{
				_Pitch = value;
				AL.Source(Source, ALSourcef.Pitch, _Pitch);
				Util.CheckOpenAlErrors();
			}
		}

		public float Gain
		{
			get
			{
				return _Gain;
			}
			set
			{
				_Gain = value;
				AL.Source(Source, ALSourcef.Gain, _Gain);
				Util.CheckOpenAlErrors();
			}
		}

		public float RolloffFactor
		{
			get
			{
				return _RolloffFactor;
			}
			set
			{
				_RolloffFactor = value;
				AL.Source(Source, ALSourcef.RolloffFactor, _RolloffFactor);
				Util.CheckOpenAlErrors();
			}
		}
	}
}
