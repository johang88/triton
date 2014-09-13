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
	class MaterialsGame : Triton.Samples.BaseGame
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
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0);

			var floor = GameWorld.CreateGameObject();
			floor.AddComponent(new Mesh { Filename = "/models/floor" });
			floor.AddComponent(new BoxRigidBody { Height = 0.01f, Width = 40.0f, Length = 40.0f, IsStatic = true });
			GameWorld.Add(floor);

			Player = GameWorld.CreateGameObject();
			Player.Position = new Vector3(0, 2f, 0);
			PlayerCharacter = new CharacterController();
			Player.AddComponent(PlayerCharacter);
			GameWorld.Add(Player);

			var materials = new string[]
			{
				"/materials/sphere",
				"/materials/gold",
				"/materials/iron",
				"/materials/wood",
			};

			for (int i = 0; i < 5; i++)
			{
				var materialName = materials[i % materials.Length];

				var cube = GameWorld.CreateGameObject();
				cube.Position = new Vector3(-3 + i * 1.5f, 1.0f, 2);
				cube.AddComponent(new Mesh { Filename = "/models/crate", MeshParameters = materialName });
				GameWorld.Add(cube);
			}

			for (int i = 0; i < 5; i++)
			{
				var materialName = materials[i % materials.Length];

				var cube = GameWorld.CreateGameObject();
				cube.Position = new Vector3(-3 + i * 1.5f, 1.0f, -2);
				cube.AddComponent(new Mesh { Filename = "/models/sphere", MeshParameters = materialName });
				GameWorld.Add(cube);
			}

			{
				var sphere = GameWorld.CreateGameObject();
				sphere.Position = new Vector3(0, 2.5f, 0);
				sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
				sphere.AddComponent(new Mesh { Filename = "/models/sphere", MeshParameters = "/materials/light_sphere", CastShadows = false });
				sphere.AddComponent(new PointLight { Color = new Vector3(1f, 0.8f, 0.5f), Intensity = 2 });
				GameWorld.Add(sphere);
			}

			//Stage.AddMesh("/models/skybox");
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
