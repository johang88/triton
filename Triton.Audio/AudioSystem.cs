using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio;

namespace Triton.Audio
{
	public class AudioSystem : IDisposable
	{
		private readonly AudioContext Context;
		private readonly DecoderFactory DecoderFactory;
		private readonly List<AudioSource> Sources;
		private readonly AudioBufferManager AudioBufferManager;
		private float[] Orientation = new float[6];

		public AudioSystem(IO.FileSystem fileSystem)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			Context = new AudioContext();
			Util.CheckOpenAlErrors();

			AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
			Util.CheckOpenAlErrors();

			DecoderFactory = new DecoderFactory();
			DecoderFactory.Register(".ogg", data => new Decoders.OggDecoder(data));

			AudioBufferManager = new AudioBufferManager(DecoderFactory, fileSystem);
			Sources = new List<AudioSource>();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose any remaining sources
				foreach (var source in Sources)
				{
					source.Dispose();
				}

				AudioBufferManager.Dispose();
				Context.Dispose();
			}
		}

		AudioSource CreateAudioSource(string filename)
		{
			var buffer = AudioBufferManager.GetBuffer(filename);
			var source = new AudioSource(buffer);

			Sources.Add(source);

			return source;
		}

		/// <summary>
		/// Play a sound
		/// </summary>
		/// <param name="filename">Name of the sound source to play</param>
		/// <returns>A reference to the audio source used to play the sound</returns>
		public IAudioSource PlaySound(string filename)
		{
			var source = CreateAudioSource(filename);
			source.Play();

			return source;
		}

		/// <summary>
		/// Play a 2d sound
		/// 
		/// A 2d sound can be played manually by setting
		///		IAudioSource.Relative = true
		///		IAudioSource.RolloffFactor = 0
		/// </summary>
		/// <param name="filename">Name of the sound source to play</param>
		/// <returns>A reference to the audio source used to play the sound</returns>
		public IAudioSource PlaySound2D(string filename)
		{
			var source = CreateAudioSource(filename);
			source.Relative = true;
			source.RolloffFactor = 0.0f;
			source.Play();

			return source;
		}

		/// <summary>
		/// Updates all audio sources
		/// </summary>
		public void Update()
		{
			foreach (var source in Sources)
			{
				source.Update();
			}
		}

		public void SetListenerPosition(Vector3 position, Vector3 forward, Vector3 up)
		{
			Orientation[0] = forward.X;
			Orientation[1] = forward.Y;
			Orientation[2] = forward.Z;
			Orientation[3] = up.X;
			Orientation[4] = up.Y;
			Orientation[5] = up.Z;

			AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
			AL.Listener(ALListenerfv.Orientation, ref Orientation);
			Util.CheckOpenAlErrors();
		}
	}
}
