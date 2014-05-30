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
	class PhysicsGame : Triton.Game.Game
	{
		private GameObject Player;
		private CharacterController PlayerCharacter;

		const float MovementSpeed = 5.0f;
		const float MouseSensitivity = 0.0025f;

		private float CameraYaw = 0;
		private float CameraPitch = 0;

		private bool IsMouseLeftDown = false;

		public PhysicsGame()
			: base("Physics")
		{
			Width = 1280;
			Height = 720;
		}

		protected override SharpFileSystem.IFileSystem MountFileSystem()
		{
			return new SharpFileSystem.FileSystems.MergedFileSystem(
				new SharpFileSystem.FileSystems.PhysicalFileSystem("../Data/core_data/"),
				new SharpFileSystem.FileSystems.PhysicalFileSystem("../Data/samples_data/")
				);
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0);

			var floor = GameWorld.CreateGameObject();
			floor.AddComponent(new Mesh { Filename = "/models/floor" });
			floor.AddComponent(new BoxRigidBody { Height = 0.01f, Width = 20.0f, Length = 20.0f, IsStatic = true });
			GameWorld.Add(floor);

			Player = GameWorld.CreateGameObject();
			Player.Position = new Vector3(0, 2f, 0);
			PlayerCharacter = new CharacterController();
			Player.AddComponent(PlayerCharacter);
			GameWorld.Add(Player);

			for (int i = 0; i < 5; i++)
			{
				var crate = GameWorld.CreateGameObject();
				crate.Position = new Vector3(-3 + i * 1.5f, 0.5f, 2);
				crate.AddComponent(new Mesh { Filename = "/models/crate" });
				crate.AddComponent(new BoxRigidBody());
				GameWorld.Add(crate);
			}

			// Light it
			Stage.CreatePointLight(
				position: new Vector3(2, 3, 0),
				range: 6.0f,
				color: new Vector3(0.4f, 0.3f, 0.9f),
				castShadows: true,
				intensity: 6
				);

			Stage.CreatePointLight(
				position: new Vector3(-2, 3, 0),
				range: 6.0f,
				color: new Vector3(0.4f, 0.9f, 0.3f),
				castShadows: true,
				intensity: 6
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

			if (InputManager.IsMouseButtonDown(MouseButton.Left))
			{
				IsMouseLeftDown = true;
			}
			else if (IsMouseLeftDown)
			{
				IsMouseLeftDown = false;

				var crate = GameWorld.CreateGameObject();
				crate.Position = Player.Position + Vector3.Transform(new Vector3(0, -0.5f, 1.5f), Camera.Orientation);
				crate.AddComponent(new Mesh { Filename = "/models/crate" });
				crate.AddComponent(new BoxRigidBody());
				GameWorld.Add(crate);

				var force = Vector3.Transform(new Vector3(0, 200, 800), Camera.Orientation);
				crate.GetComponent<BoxRigidBody>().AddForce(force);
			}
		}
	}
}
