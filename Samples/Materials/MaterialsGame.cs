using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Input;

namespace Triton.Samples
{
	class MaterialsGame : Triton.Game.Game
	{
		private GameObject Player;
		private CharacterController PlayerCharacter;

		const float MovementSpeed = 5.0f;
		const float MouseSensitivity = 0.0025f;

		private float CameraYaw = 0;
		private float CameraPitch = 0;

		public MaterialsGame()
			: base("Materials")
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

			var floor = GameWorld.CreateGameObject();
			floor.AddComponent(new Mesh { Filename = "models/floor" });
			floor.AddComponent(new BoxRigidBody { Height = 0.01f, Width = 40.0f, Length = 40.0f, IsStatic = true });
			GameWorld.Add(floor);

			Player = GameWorld.CreateGameObject();
			Player.Position = new Vector3(0, 2f, 0);
			PlayerCharacter = new CharacterController();
			Player.AddComponent(PlayerCharacter);
			GameWorld.Add(Player);

			for (int i = 0; i < 5; i++)
			{
				var sphere = GameWorld.CreateGameObject();
				sphere.Position = new Vector3(-3 + i * 1.5f, 1.0f, 2);
				sphere.AddComponent(new Mesh { Filename = "models/sphere" });
				GameWorld.Add(sphere);
			}

			for (int i = 0; i < 5; i++)
			{
				var sphere = GameWorld.CreateGameObject();
				sphere.Position = new Vector3(-3 + i * 1.5f, 1.0f, -2);
				sphere.AddComponent(new Mesh { Filename = "models/sphere" });
				GameWorld.Add(sphere);
			}

			Stage.CreatePointLight(
					position: new Vector3(0, 4, 0),
					range: 8.0f,
					color: new Vector3(0.8f, 0.8f, 0.83f),
					castShadows: true,
					intensity: 2
				);

			DebugFlags |= Game.DebugFlags.RenderStats;
		}

		protected override void Update(float frameTime)
		{
			base.Update(frameTime);

			if (InputManager.IsKeyDown(Triton.Input.Key.Escape))
			{
				Running = false;
			}

			// Player input
			var movement = Vector3.Zero;
			if (InputManager.IsKeyDown(Key.W))
				movement.Z = 1.0f;
			else if (InputManager.IsKeyDown(Key.S))
				movement.Z = -1.0f;

			if (InputManager.IsKeyDown(Key.A))
				movement.X = 1.0f;
			else if (InputManager.IsKeyDown(Key.D))
				movement.X = -1.0f;

			if (movement.LengthSquared > 0.0f)
			{
				movement = movement.Normalize();
			}

			var movementDir = Quaternion.FromAxisAngle(Vector3.UnitY, CameraYaw);
			movement = Vector3.Transform(movement * MovementSpeed, movementDir);

			CameraYaw += -InputManager.MouseDelta.X * MouseSensitivity;
			CameraPitch += InputManager.MouseDelta.Y * MouseSensitivity;

			Camera.Orientation = Quaternion.Identity;
			Camera.Yaw(CameraYaw);
			Camera.Pitch(CameraPitch);

			PlayerCharacter.Move(movement, InputManager.IsKeyDown(Key.Space));
			Camera.Position = Player.Position;
		}
	}
}
