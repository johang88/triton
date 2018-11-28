using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Graphics.Resources;
using Triton.Input;
using Triton.Samples.Components;

namespace Triton.Samples
{
    class MaterialsGame : Triton.Samples.BaseGame
    {
        private GameObject Player;
        private CharacterController PlayerCharacter;
        
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
            PlayerCharacter = new CharacterController();
            Player.Components.Add(PlayerCharacter);
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

            // Setup a test trigger
            var bigBall = new GameObject();
            bigBall.Components.Add(new BoxRigidBody
            {
                IsStatic = true,
                IsTrigger = true,
                Length = 2.5f,
                Width = 2.5f,
                Height = 2.5f
            });
            bigBall.Components.Add(new PointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = 100,
                Enabled = false,
                Range = 5f
            });
            bigBall.Components.Add(new TriggerTest());
            bigBall.Scale = new Vector3(5, 5, 5);
            bigBall.Position = new Vector3(5, 2.5f, 5);

            GameWorld.Add(bigBall);

            Resources.Unload(roomPrefab);
            Resources.Unload(ballPrefab);

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

        private Random rng = new Random();
        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 100), Condition.Always);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(RequestedWidth - 500, RequestedHeight - 110), Condition.Always, System.Numerics.Vector2.Zero);
            ImGui.BeginWindow("Material Game!!1", WindowFlags.NoResize | WindowFlags.NoMove | WindowFlags.NoCollapse);

            if (ImGui.Button("Destroy all Ballz"))
            {
                foreach (var ball in _balls)
                {
                    GameWorld.Remove(ball);
                }
                _balls.Clear();
            }


            if (ImGui.Button("Add some ball!"))
            {
                var ballPrefab = Resources.Load<Prefab>("/prefabs/ball");

                var ball = ballPrefab.Instantiate(GameWorld);
                _balls.Add(ball);

                ball.Position = Player.Position + Vector3.Transform(new Vector3(0, 1, 1), Player.Orientation);
                ball.Orientation = Player.Orientation;

                Resources.Unload(ballPrefab);
            }

            if (ImGui.Button("Add some Force!"))
            {
                foreach (var ball in _balls)
                {
                    var rigidBody = ball.GetComponent<RigidBody>();
                    rigidBody.AddForce((new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()) - new Vector3(0.5f, 0.5f, 0.5f)) * 10.0f);
                }
            }

            ImGui.EndWindow();
        }
    }
}
