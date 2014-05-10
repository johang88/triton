using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Audio
{
	public interface IAudioSource : IDisposable
	{
		/// <summary>
		/// Start playback of the source
		/// </summary>
		void Play();

		/// <summary>
		/// Stop playback of the source
		/// </summary>
		void Stop();

		/// <summary>
		/// Pauses playback
		/// </summary>
		void Pause();

		/// <summary>
		/// Sets the playback position to zero
		/// </summary>
		void Rewind();

		/// <summary>
		/// True if the source is currently playing
		/// </summary>
		bool IsPlaying { get; }

		/// <summary>
		/// True if the source is looping, false otherwise
		/// </summary>
		bool Loop { get; set; }

		/// <summary>
		/// The sounds position in the world
		/// </summary>
		Vector3 Position { get; set; }

		/// <summary>
		/// Velocity of the source
		/// </summary>
		Vector3 Velocity { get; set; }

		/// <summary>
		/// The maximum distance where the sound will be audible
		/// </summary>
		float Range { get; set; }

		/// <summary>
		/// Indiciates whether source is relative, ie all parameters are relative to the source instead of the listener,
		/// usually used for background music and ui sound effects.
		/// </summary>
		bool Relative { get; set; }

		/// <summary>
		/// Controls the pitch
		/// </summary>
		float Pitch { get; set; }

		/// <summary>
		/// Controls the gain
		/// </summary>
		float Gain { get; set; }

		/// <summary>
		/// Controls the rolloff factory
		/// </summary>
		float RolloffFactor { get; set; }
	}
}
