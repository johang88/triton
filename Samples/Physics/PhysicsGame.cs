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
	class PhysicsGame : BaseGame
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
		}

		private void CreateContainer(Vector3 position)
		{
			var container = GameWorld.CreateGameObject();
			container.Position = position;
			container.AddComponent(new Mesh { Filename = "/models/container" });
			container.AddComponent(new MeshRigidBody { Filename = "/collision/container" });
			GameWorld.Add(container);
		}

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0) * 0.1f;

			{
				var city = GameWorld.CreateGameObject();
				city.AddComponent(new Mesh { Filename = "/models/city" });
				city.AddComponent(new MeshRigidBody { Filename = "/collision/city" });
				GameWorld.Add(city);
			}

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

			CreateContainer(new Vector3(0, 0, 4 + 4 * 0));
			CreateContainer(new Vector3(0, 0, 4 + 4 * 1));
			CreateContainer(new Vector3(0, 0, 4 + 4 * 2));
			CreateContainer(new Vector3(6, 0, 4 + 4 * 2));

			CreateContainer(new Vector3(0, 2, 5 + 2 * 0));
			CreateContainer(new Vector3(0, 2, 5 + 2 * 1));
			CreateContainer(new Vector3(0, 2, 5 + 2 * 2));
			CreateContainer(new Vector3(0, 2, 5 + 2 * 3));

			Stage.AmbientColor = Vector3.Zero;
			Stage.CreateDirectionalLight(new Vector3(-0.3f, -0.8f, 0.66f), new Vector3(1f, 0.8f, 0.5f), true, intensity: 1);
			Stage.CreateDirectionalLight(new Vector3(0.3f, -0.8f, -0.66f), new Vector3(0.8f, 0.7f, 1.0f), false, intensity: 0.2f);
			Camera.FarClipDistance = 200;

			DebugFlags |= Game.DebugFlags.RenderStats;

			PostEffectManager.HDRSettings.EnableBloom = true;
			PostEffectManager.HDRSettings.AdaptationRate = 2;

			DeferredRenderer.Settings.ShadowQuality = Graphics.Deferred.ShadowQuality.Medium;
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

			if (InputManager.WasKeyPressed(Key.ControlLeft))
			{
				var crate = GameWorld.CreateGameObject();
				crate.Position = Player.Position + Vector3.Transform(new Vector3(0, -0.5f, 1.5f), Camera.Orientation);
				crate.AddComponent(new Mesh { Filename = "/models/crate" });
				crate.AddComponent(new BoxRigidBody());
				GameWorld.Add(crate);

				var force = Vector3.Transform(new Vector3(0, 200, 800), Camera.Orientation);
				crate.GetComponent<BoxRigidBody>().AddForce(force);
			}

			if (InputManager.IsMouseButtonDown(MouseButton.Left))
			{
				IsMouseLeftDown = true;
			}
			else if (IsMouseLeftDown)
			{
				IsMouseLeftDown = false;

				var sphere = GameWorld.CreateGameObject();
				sphere.Position = Player.Position + Vector3.Transform(new Vector3(0, -0.5f, 1.5f), Camera.Orientation);
				sphere.AddComponent(new Mesh { Filename = "/models/sphere", Material = "/materials/light_sphere", CastShadows = false });
				sphere.AddComponent(new PointLight { Range = 2 + (float)RNG.NextDouble() * 8, CastShadows = true, Color = new Vector3(0.1f + (float)RNG.NextDouble() * 0.9f, 0.1f + (float)RNG.NextDouble() * 0.9f, 0.1f + (float)RNG.NextDouble() * 0.9f), Intensity = 0.2f + (float)RNG.NextDouble() * 1.5f });
				sphere.AddComponent(new SphereRigidBody { Radius = 0.4f });
				GameWorld.Add(sphere);

				var force = Vector3.Transform(new Vector3(0, 100, 200), Camera.Orientation);
				sphere.GetComponent<SphereRigidBody>().AddForce(force);
			}
		}

		private bool RaycastCallback(Physics.Body body, Vector3 normal, float fraction)
		{
			return PlayerCharacter != null && body != PlayerCharacter.Body;
		}
	}
}
