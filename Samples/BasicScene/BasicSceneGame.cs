﻿using System;
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

		public BasicSceneGame()
			: base("BasicGame")
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
				new Triton.Vector3(0, 4, 3),
				new Triton.Vector3(0, -1, 0.3f),
				0.6f, 0.9f, 6.0f,
				new Triton.Vector3(0.9f, 0.5f, 0.3f), true, 0.05f);
			Light.Intensity = LightBaseIntensity;

			// Setup the camera
			Camera.Position = new Triton.Vector3(0, 2, 0);
			Camera.Pitch(0.7f);
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
			Crate.World = Triton.Matrix4.CreateRotationY(CrateRotation) * Triton.Matrix4.CreateTranslation(0, 0, 3.0f);

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
