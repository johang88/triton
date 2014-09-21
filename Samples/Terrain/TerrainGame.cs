using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Input;

namespace Triton.Samples
{
	class TerrainGame : Triton.Samples.BaseGame
	{
		private Vector3 Position = Vector3.Zero;

		const float MovementSpeed = 10.0f;
		const float MouseSensitivity = 0.0025f;

		private float CameraYaw = 0;
		private float CameraPitch = 0;

		private Graphics.Terrain.Terrain Terrain;

		public TerrainGame()
			: base("Terrain")
		{

		}

		protected override void LoadResources()
		{
			base.LoadResources();

			using (var stream = FileSystem.OpenRead("/terrain.raw"))
			{
				var material = GameResources.Load<Graphics.Resources.Material>("/materials/terrain");
				var compositeMaterial = GameResources.Load<Graphics.Resources.Material>("/materials/terrain_composite");

				var terrainData = new Graphics.Terrain.TerrainData(stream, new Vector3(4096, 2048, 4096), 513);
				Terrain = new Graphics.Terrain.Terrain(GraphicsBackend, terrainData, 10, 64, material, compositeMaterial);

				Stage.AddMesh(Terrain.Mesh);
			}

			string[] meshes = new string[] { "/models/house", "/models/house2" };
			var random = new Random();

			Camera.Position = new Vector3(128, 128, 128);
			Camera.FarClipDistance = 10000;

			Stage.CreateDirectionalLight(new Vector3(-0.339778f, -0.95f, 0.425624f), new Vector3(1.18f, 1.18f, 1.12f), false, intensity: 4);
			Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0) * 4;

			DeferredRenderer.FogSettings.Color = new Triton.Vector3(185 / 255.0f, 224 / 255.0f, 239 / 255.0f) * 4;
			DeferredRenderer.FogSettings.Start = 200.0f;
			DeferredRenderer.FogSettings.End = 2000.0f;
			DeferredRenderer.FogSettings.Enable = true;

			//HDRRenderer.KeyValue = 0.735f;
			//HDRRenderer.AdaptationRate = 0.6f;

			//Stage.SetSkyBox("/models/skybox");

			DebugFlags |= Game.DebugFlags.RenderStats;
		}

		protected override void Update(float frameTime)
		{
			base.Update(frameTime);

			if (InputManager.IsKeyDown(Triton.Input.Key.Escape))
			{
				Running = false;
			}

			Terrain.Update(Camera.Position);

			var movement = Vector3.Zero;
			if (InputManager.IsKeyDown(Key.W))
				movement.Z = 1.0f;
			else if (InputManager.IsKeyDown(Key.S))
				movement.Z = -1.0f;

			if (InputManager.IsKeyDown(Key.A))
				movement.X = 1.0f;
			else if (InputManager.IsKeyDown(Key.D))
				movement.X = -1.0f;

			CameraYaw += -InputManager.MouseDelta.X * MouseSensitivity;
			CameraPitch += InputManager.MouseDelta.Y * MouseSensitivity;

			Camera.Orientation = Quaternion.Identity;
			Camera.Yaw(CameraYaw);
			Camera.Pitch(CameraPitch);

			var speed = MovementSpeed;
			if (InputManager.IsKeyDown(Key.ShiftLeft))
				speed *= 10;

			Camera.Position += Vector3.Transform(movement * speed * frameTime, Camera.Orientation);

			if (!InputManager.IsKeyDown(Key.Space))
				Camera.Position.Y = Terrain.Data.GetHeightAt(Camera.Position.X, Camera.Position.Z) + 1.7f;
		}
	}
}
