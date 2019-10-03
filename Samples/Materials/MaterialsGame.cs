using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Graphics.Components;
using Triton.Graphics.Resources;
using Triton.Input;
using Triton.Physics.Components;
using Triton.Samples.Components;
using Triton.Terrain;

namespace Triton.Samples
{
    class MaterialsGame : Triton.Samples.BaseGame
    {
        private readonly List<GameObject> _balls = new List<GameObject>();
        private GameObject _player;

        public MaterialsGame()
            : base("Materials")
        {
            CursorVisible = false;

            RequestedWidth = 1920;
            RequestedHeight = 1080;
        }

        protected override void LoadResources()
        {
            base.LoadResources();

            Stage.ClearColor = new Triton.Vector4(255 / 255.0f, 255 / 255.0f, 255 / 255.0f, 0) * 0;
            Stage.AmbientColor = new Vector3(0.5f, 0.5f, 0.5f) * 0;
            Stage.Camera.FarClipDistance = 512.0f;

            PostEffectManager.HDRSettings.AutoKey = true;
            PostEffectManager.HDRSettings.TonemapOperator = Graphics.Post.TonemapOperator.ASEC;

            Stage.AmbientLight = new Graphics.AmbientLight
            {
                Irradiance = Resources.Load<Graphics.Resources.Texture>("/textures/sky_irradiance"),
                Specular = Resources.Load<Graphics.Resources.Texture>("/textures/sky_specular"),
                IrradianceStrength = 2,
                SpecularStrength = 2
            };

            _player = new GameObject
            {
                Position = new Vector3(2, 0, 2)
            };
            _player.Components.Add(new CharacterControllerComponent
            {
                ColliderShape = new Triton.Physics.Shapes.CapsuleColliderShape
                {
                    Height = 1.5f,
                    Radius = 0.15f
                }
            });
            _player.Components.Add(new PlayerController
            {
            });
            _player.Components.Add(new ThirdPersonCamera());

            var knight = new GameObject
            {
                Position = Vector3.Zero,
                Scale = new Vector3(0.024f, 0.024f, 0.024f)
            };
            knight.Components.Add(new SkinnedMeshComponent
            {
                Mesh = Resources.Load<Mesh>("/models/knight_test")
            });
            knight.Components.Add(new KnightAnimator());
            _player.Children.Add(knight);

            GameWorld.Add(_player);

            var roomPrefab = Resources.Load<Prefab>("/prefabs/room");
            roomPrefab.Instantiate(GameWorld);

            var center = new Vector3(0, 1, 0);
            var ballPrefab = Resources.Load<Prefab>("/prefabs/ball");
            for (int i = 0; i < 5; i++)
            {
                var ball = ballPrefab.Instantiate(GameWorld);
                _balls.Add(ball);
                ball.Position = center + new Vector3(-3 + i * 1.5f, 1.2f, 2);
            }

            for (int i = 0; i < 5; i++)
            {
                var ball = ballPrefab.Instantiate(GameWorld);
                _balls.Add(ball);
                ball.Position = center + new Vector3(-3 + i * 1.5f, 0.3f, -2);
            }

            Stage.ClearColor = new Vector4(1, 1, 1, 1) * 2;

            DeferredRenderer.Settings.ShadowQuality = Graphics.Deferred.ShadowQuality.High;
            DebugFlags |= Game.DebugFlags.RenderStats;
            DebugFlags |= Game.DebugFlags.ShadowMaps;
        }

        protected override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (InputManager.WasKeyPressed(Triton.Input.Key.Escape))
            {
                CursorVisible = !CursorVisible;
            }
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);
        }
    }
}
