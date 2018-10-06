using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
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

            Player = new GameObject();
            Player.Position = new Vector3(0, 2f, 0);
            PlayerCharacter = new CharacterController();
            Player.Components.Add(PlayerCharacter);
            GameWorld.Add(Player);

            var roomPrefab = Resources.Load<Prefab>("/prefabs/room");
            roomPrefab.Instantiate(GameWorld);

            var ballPrefab = Resources.Load<Prefab>("/prefabs/ball");
            for (int i = 0; i < 5; i++)
            {
                ballPrefab.Instantiate(GameWorld).Position = new Vector3(-3 + i * 1.5f, 1.5f, 2);
            }

            for (int i = 0; i < 5; i++)
            {
                ballPrefab.Instantiate(GameWorld).Position = new Vector3(-3 + i * 1.5f, 1.5f, -2);
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
