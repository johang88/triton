using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Samples
{
	class BasicSceneGame : Triton.Game.Game
	{
		private const float CrateRotationSpeed = 1.23f;
		private Triton.Graphics.MeshInstance Crate;
		private float CrateRotation = 0.0f;
		private Triton.Graphics.Light Light;

		private float LightFlickerDirection = 1.0f;
		private float LightBaseIntensity = 1.4f;
		private float LightFlickerCutoff = 0.3f;

		private Random RNG = new Random();

		private Vector3 CratePosition = new Vector3(0, 0, 3.0f);

		public BasicSceneGame()
			: base("BasicScene")
		{
			Width = 1280;
			Height = 720;
		}

		protected override void MountFileSystem()
		{
			base.MountFileSystem();

			// Mount the core resource package, this is required
			FileSystem.AddPackage("FileSystem", "../Data/core_data");

			// Mount the sample data
			FileSystem.AddPackage("FileSystem", "../Data/samples_data");
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(0.5f, 0.5f, 0.7f, 0);

			// Create our awesome main actor :)
			Crate = Stage.AddMesh("models/crate");

			// Create a "floor"
			var floor = Stage.AddMesh("models/crate");
			floor.World = Triton.Matrix4.Scale(10, 1, 10) * Triton.Matrix4.CreateTranslation(0, -1, 4);

			// Light it
			Light = Stage.CreateSpotLight(
				position: new Triton.Vector3(0, 4, 3),
				direction: new Triton.Vector3(0, -1, 0.3f),
				innerAngle: 0.6f, 
				outerAngle: 0.9f, 
				range: 6.0f,
				color: new Triton.Vector3(0.9f, 0.5f, 0.3f), 
				castShadows: true, 
				shadowBias: 0.05f,
				intensity: LightBaseIntensity);

			Stage.CreatePointLight(
				position: new Vector3(2, 1, 3),
				range: 2.0f,
				color: new Vector3(0.4f, 0.3f, 0.9f),
				castShadows: false,
				intensity: 2
				);

			Stage.CreatePointLight(
				position: new Vector3(-2, 1, 3),
				range: 2.0f,
				color: new Vector3(0.4f, 0.9f, 0.3f),
				castShadows: false,
				intensity: 2
				);

			// Setup the camera
			Camera.Position = new Triton.Vector3(0, 2, 0);
			Camera.Pitch(0.7f);

			DebugFlags = Game.DebugFlags.RenderStats;
		}

		protected override void Update(float frameTime)
		{
			base.Update(frameTime);

			if (InputManager.IsKeyDown(Triton.Input.Key.Escape))
			{
				Running = false;
			}

			// Rotate the crate around it's origin and setup the world matrix
			CrateRotation += CrateRotationSpeed * frameTime;

			var cratePos = CratePosition + new Vector3(0, 0.5f + 0.5f * (float)System.Math.Sin(ElapsedTime), 1 + 1.0f * (float)System.Math.Sin(ElapsedTime));
			Crate.World = Triton.Matrix4.CreateRotationY(CrateRotation) * Triton.Matrix4.CreateTranslation(cratePos);

			// Flickering light
			Light.Intensity += LightFlickerDirection * 1.5f * frameTime;
			if ((LightFlickerDirection > 0 && Light.Intensity > LightBaseIntensity + LightFlickerCutoff)
				|| (Light.Intensity < LightBaseIntensity - LightFlickerCutoff))
			{
				Light.Intensity = LightFlickerDirection > 0 ? LightBaseIntensity + LightFlickerCutoff : LightBaseIntensity - LightFlickerCutoff;

				LightFlickerDirection = LightFlickerDirection * -1.0f;
				LightFlickerCutoff = (float)(0.1f + RNG.NextDouble() * 0.6f);
			}
		}
	}
}
