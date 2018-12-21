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

namespace Triton.Samples
{
    class MaterialsGame : Triton.Samples.BaseGame
    {
        private GameObject Player;
        private GameObject Light;
        private KnightAnimator _animator;

        private List<GameObject> _balls = new List<GameObject>();

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

            PostEffectManager.HDRSettings.AutoKey = true;
            PostEffectManager.HDRSettings.TonemapOperator = Graphics.Post.TonemapOperator.ASEC;

            Stage.AmbientLight = new Graphics.AmbientLight
            {
                Irradiance = Resources.Load<Graphics.Resources.Texture>("/textures/sky_irradiance"),
                Specular = Resources.Load<Graphics.Resources.Texture>("/textures/sky_specular")
            };

            Player = new GameObject();
            Player.Position = new Vector3(0, 2f, 0);
            Player.Components.Add(new CharacterControllerComponent
            {
                ColliderShape = new Triton.Physics.Shapes.CapsuleColliderShape
                {
                    Height = 1.5f,
                    Radius = 0.15f
                }
            });
            Player.Components.Add(new PlayerController());
            GameWorld.Add(Player);

            var roomPrefab = Resources.Load<Prefab>("/prefabs/city");
            roomPrefab.Instantiate(GameWorld);

            var ballPrefab = Resources.Load<Prefab>("/prefabs/ball");
            for (int i = 0; i < 5; i++)
            {
                var ball = ballPrefab.Instantiate(GameWorld);
                _balls.Add(ball);
                ball.Position = new Vector3(-3 + i * 1.5f, 1.5f, 2);
            }

            for (int i = 0; i < 5; i++)
            {
                var ball = ballPrefab.Instantiate(GameWorld);
                _balls.Add(ball);
                ball.Position = new Vector3(-3 + i * 1.5f, 1.5f, -2);
            }

            var knight = new GameObject();
            knight.Position = new Vector3(1, 0, 4);
            knight.Scale = new Vector3(0.024f, 0.024f, 0.024f);
            //knight.Scale = new Vector3(1.6f, 1.6f, 1.6f);
            knight.Components.Add(new SkinnedMeshComponent
            {
                Mesh = Resources.Load<Mesh>("/models/knight_test")
            });
            _animator = new KnightAnimator();
            knight.Components.Add(_animator);
            GameWorld.Add(knight);

            Light = new GameObject
            {
                Position = new Vector3(0, 0.4f, 0),
                Orientation = Quaternion.FromAxisAngle(Vector3.UnitX, 0.45f) * Quaternion.FromAxisAngle(Vector3.UnitY, -0.35f)
            };
            Light.Components.Add(new LightComponent
            {
                Type = Graphics.LighType.Directional,
                Intensity = 10,
                Color = new Vector3(1.1f, 1, 1),
                Range = 100,
                InnerAngle = 0.94f,
                OuterAngle = 0.98f,
                CastShadows = true
            });
            GameWorld.Add(Light);

            Stage.ClearColor = new Vector4(1, 1, 1, 1) * 2;

            //var sunLight = Stage.CreateDirectionalLight(new Vector3(-0.3f, -0.7f, 0.66f), new Vector3(1.64f, 1.57f, 1.49f), true, shadowBias: 0.0025f, intensity: 2f);
            //sunLight.Enabled = true;

            DeferredRenderer.Settings.ShadowQuality = Graphics.Deferred.ShadowQuality.High;
            DebugFlags |= Game.DebugFlags.RenderStats;
            //DebugFlags |= Game.DebugFlags.ShadowMaps;
        }

        protected override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (InputManager.WasKeyPressed(Triton.Input.Key.Escape))
            {
                CursorVisible = !CursorVisible;
            }

            //Light.Orientation *= Quaternion.FromAxisAngle(Vector3.UnitY, frameTime);
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);
        }
    }
}
