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

            Stage.ClearColor = new Triton.Vector4(185 / 255.0f, 224 / 255.0f, 239 / 255.0f, 0);
            Stage.AmbientColor = Vector3.Zero;

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

            // Create a shit load of point lights
            var lightSpacing = 0.6f;
            var numLights = 40;
            for (var x = 0; x < numLights; x++)
            {
                for (var z = 0; z < numLights; z++)
                {
                    var position = new Vector3(
                        (-lightSpacing * numLights * 0.5f) + lightSpacing * x,
                        (float)rng.NextDouble(),
                        (-lightSpacing * numLights * 0.5f) + lightSpacing * z
                        );

                    Stage.CreatePointLight(position, 2.0f + (float)rng.NextDouble(), new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble()), intensity: 0.1f + (float)rng.NextDouble() * 0.5f);
                }
            }

            Resources.Unload(roomPrefab);
            Resources.Unload(ballPrefab);

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
