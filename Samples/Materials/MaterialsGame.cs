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

        const float MovementSpeed = 3.5f;
        const float MouseSensitivity = 0.0025f;

        private float CameraYaw = 0;
        private float CameraPitch = 0;

        public MaterialsGame()
            : base("Materials")
        {
            CursorVisible = false;
        }

        protected override void LoadResources()
        {
            base.LoadResources();

            Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0);
            Stage.AmbientColor = Vector3.Zero;

            var room = new GameObject();
            room.Components.Add(new Mesh { Filename = "/models/room" });
            room.Components.Add(new MeshRigidBody { Filename = "/collision/room", IsStatic = true });
            GameWorld.Add(room);

            Player = new GameObject();
            Player.Position = new Vector3(0, 2f, 0);
            PlayerCharacter = new CharacterController();
            Player.Components.Add(PlayerCharacter);
            GameWorld.Add(Player);

            var materials = new string[]
            {
                "/materials/sphere",
                //"/materials/gold",
                //"/materials/iron",
                //"/materials/wood",
            };

            var rng = new System.Random();

            for (int i = 0; i < 5; i++)
            {
                var materialName = materials[i % materials.Length];

                var cube = new GameObject();
                cube.Position = new Vector3(-3 + i * 1.5f, 1.5f, 2);
                cube.Components.Add(new Mesh { Filename = "/models/sphere", Material = materialName });
                cube.Components.Add(new SphereRigidBody { Radius = 0.5f });
                GameWorld.Add(cube);
            }

            for (int i = 0; i < 5; i++)
            {
                var materialName = materials[i % materials.Length];

                var cube = new GameObject();
                cube.Position = new Vector3(-3 + i * 1.5f, 1.5f, -2);
                cube.Components.Add(new Mesh { Filename = "/models/sphere", Material = materialName });
                cube.Components.Add(new SphereRigidBody { Radius = 0.5f });
                GameWorld.Add(cube);
            }

            var lightColor = new Vector3(1.1f, 1f, 1f);

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(2, 2.5f, 0);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = lightColor, Intensity = 2, Range = 16 });
                GameWorld.Add(sphere);
            }

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(6, 2.5f, 0);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = lightColor, Intensity = 1, Range = 16, CastShadows = false });
                GameWorld.Add(sphere);
            }

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(2, 2.5f, -4);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = lightColor, Intensity = 1, Range = 16, CastShadows = false });
                GameWorld.Add(sphere);
            }

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(2, 2.5f, 4);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = lightColor, Intensity = 1, Range = 16, CastShadows = false });
                GameWorld.Add(sphere);
            }

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(-10, 2.5f, 1);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = new Vector3(1f, 1f, 1f), Intensity = 2 });
                GameWorld.Add(sphere);
            }

            {
                var sphere = new GameObject();
                sphere.Position = new Vector3(-18, 2.5f, -5);
                sphere.Scale = new Vector3(1, 1, 1) * 0.15f;
                sphere.Components.Add(new PointLight { Color = new Vector3(1f, 1f, 1f), Intensity = 1.5f, Range = 8 });
                GameWorld.Add(sphere);
            }

            for (var i = 0; i < 0; i++)
            {
                var x = (float)(-6.0 + RNG.NextDouble() * 15.0);
                var z = (float)(-8.0 + RNG.NextDouble() * 20.0);

                var sphere = new GameObject();
                sphere.Position = new Vector3(x, 0.5f, z);
                sphere.Components.Add(new PointLight
                {
                    Color = new Vector3(0.1f + (float)RNG.NextDouble() * 0.9f, 0.1f + (float)RNG.NextDouble() * 0.9f, 0.1f + (float)RNG.NextDouble() * 0.9f),
                    Intensity = 0.3f + (float)RNG.NextDouble() * 0.6f,
                    Range = 0.7f + (float)RNG.NextDouble() * 0.9f,
                    CastShadows = false
                });
                GameWorld.Add(sphere);
            }

            DeferredRenderer.Settings.ShadowQuality = Graphics.Deferred.ShadowQuality.High;

            DebugFlags |= Game.DebugFlags.RenderStats;
        }

        protected override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (InputManager.WasKeyPressed(Triton.Input.Key.Escape))
            {
                CursorVisible = !CursorVisible;
            }

            if (!CursorVisible)
            {
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
                Camera.Position = Player.Position + new Vector3(0, 0.7f, 0);
            }
        }
    }
}
