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

        private List<GameObject> _balls = new List<GameObject>();

        public MaterialsGame()
            : base("Materials")
        {
            CursorVisible = false;
        }

        protected override void LoadResources()
        {
            base.LoadResources();

            Stage.ClearColor = new Triton.Vector4(255 / 255.0f, 255 / 255.0f, 255 / 255.0f, 0) * 3;
            Stage.AmbientColor = new Vector3(0.5f, 0.5f, 0.5f);

            PostEffectManager.HDRSettings.AutoKey = false;
            PostEffectManager.HDRSettings.TonemapOperator = Graphics.Post.TonemapOperator.ASEC;

            //Stage.AmbientLight = new Graphics.AmbientLight
            //{
            //    Irradiance = Resources.Load<Graphics.Resources.Texture>("/textures/sky_irradiance"),
            //    Specular = Resources.Load<Graphics.Resources.Texture>("/textures/sky_specular")
            //};

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

            var roomPrefab = Resources.Load<Prefab>("/prefabs/room");
            var ballPrefab = Resources.Load<Prefab>("/prefabs/ball");

            roomPrefab.Instantiate(GameWorld);
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
            knight.Position = new Vector3(2, 0, 2);
            knight.Scale = new Vector3(1.6f, 1.6f, 1.6f);
            knight.Components.Add(new SkinnedMeshComponent
            {
                Mesh = Resources.Load<Mesh>("/models/knight")
            });
            knight.Components.Add(new KnightAnimator());
            GameWorld.Add(knight);

            Stage.ClearColor = new Vector4(1, 1, 1, 1) * 2;

            //var sunLight = Stage.CreateDirectionalLight(new Vector3(-0.3f, -0.7f, 0.66f), new Vector3(1.64f, 1.57f, 1.49f), true, shadowBias: 0.0025f, intensity: 2f);
            //sunLight.Enabled = true;

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
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);
        }
    }
}
