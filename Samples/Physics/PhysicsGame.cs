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

		protected override void LoadResources()
		{
			base.LoadResources();

			Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0);

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

			// Light it
			//{
			//	var sphere = GameWorld.CreateGameObject();
			//	sphere.Position = new Vector3(0, 2.5f, 2);
			//	sphere.Scale = new Vector3(1, 1, 1);
			//	sphere.AddComponent(new Mesh { Filename = "/models/sphere", MeshParameters = "/materials/light_sphere", CastShadows = false });
			//	sphere.AddComponent(new PointLight { Color = new Vector3(1f, 0.8f, 0.5f), Intensity = 2 });
			//	sphere.AddComponent(new SphereRigidBody { Radius = 0.7f });
			//	GameWorld.Add(sphere);
			//}

			Stage.AmbientColor = Vector3.Zero;
			Stage.CreateDirectionalLight(new Vector3(-0.3f, -0.8f, 0.66f), new Vector3(1f, 0.8f, 0.5f), true, intensity: 2);
			Stage.CreateDirectionalLight(new Vector3(0.3f, -0.8f, -0.66f), new Vector3(0.8f, 0.7f, 1.0f), false, intensity: 0.8f);
			Camera.FarClipDistance = 200;

			//DebugFlags |= Game.DebugFlags.RenderStats;

			PostEffectManager.HDRSettings.EnableLensFlares = true;
			PostEffectManager.HDRSettings.EnableBloom = true;

			//HDRRenderer.AdaptationRate = 2;
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

				var rayPosition = Camera.Position;
				var rayDirection = Vector3.Transform(Vector3.UnitZ, Camera.Orientation);

				Physics.Body body; Vector3 normal; float fraction;
				if (PhysicsWorld.Raycast(rayPosition, rayDirection, RaycastCallback, out body, out normal, out fraction))
				{
					var force = Vector3.Transform(new Vector3(0, 200, 800), Camera.Orientation);
					body.AddForce(force);
				}
			}
		}

		private bool RaycastCallback(Physics.Body body, Vector3 normal, float fraction)
		{
			return PlayerCharacter != null && body != PlayerCharacter.Body;
		}
	}
}
