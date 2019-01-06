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
        private GameObject Player;
        private GameObject Light;

        private List<GameObject> _balls = new List<GameObject>();

        private float _rotationX = 0.43f;
        private float _rotationY = -0.38f;

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

            var terrain = new GameObject();
            var terrainData = TerrainData.CreateFromFile(FileSystem, "/terrain.raw");
            terrainData.MaxHeight = 512;
            terrain.Components.Add(new TerrainComponent
            {
                Material = Resources.Load<Material>("/materials/terrain"),
                TerrainData = terrainData
            });
            terrain.Components.Add(new RigidBodyComponent
            {
                RigidBodyType = Physics.RigidBodyType.Static,
                ColliderShape = new Triton.Physics.Shapes.TerrainColliderShape
                {
                    TerrainData = terrainData
                }
            });
            GameWorld.Add(terrain);

            Player = new GameObject();
            Player.Position = new Vector3(200, 0, 193);
            Player.Position.Y = terrainData.GetHeightAt(Player.Position.X, Player.Position.Z) + 1f;
            Player.Components.Add(new CharacterControllerComponent
            {
                ColliderShape = new Triton.Physics.Shapes.CapsuleColliderShape
                {
                    Height = 1.5f,
                    Radius = 0.15f
                }
            });
            Player.Components.Add(new PlayerController
            {
                Terrain = terrainData
            });
            GameWorld.Add(Player);

            //var roomPrefab = Resources.Load<Prefab>("/prefabs/suntemple");
            //roomPrefab.Instantiate(GameWorld);

            var center = new Vector3(200, 225, 193);
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

            //var knight = new GameObject();
            //knight.Position = new Vector3(1, 2.79f - 0.5f, 4);
            //knight.Scale = new Vector3(0.024f, 0.024f, 0.024f);
            ////knight.Scale = new Vector3(1.6f, 1.6f, 1.6f);
            //knight.Components.Add(new SkinnedMeshComponent
            //{
            //    Mesh = Resources.Load<Mesh>("/models/knight_test")
            //});
            //_animator = new KnightAnimator();
            //knight.Components.Add(_animator);
            //GameWorld.Add(knight);

            Light = new GameObject
            {
                Position = new Vector3(0, 0.4f, 0),
                Orientation = Quaternion.FromAxisAngle(Vector3.UnitX, 0.43f)
            };
            Light.Components.Add(new LightComponent
            {
                Type = Graphics.LighType.Directional,
                Intensity = 4,
                Color = new Vector3(1.1f, 1, 1),
                Range = 100,
                InnerAngle = 0.94f,
                OuterAngle = 0.98f,
                CastShadows = true
            });
            GameWorld.Add(Light);

            //var particles = new GameObject
            //{
            //    Position = new Vector3(0, 0.1f, 0),
            //};
            //particles.Components.Add(new ParticleSystemComponent
            //{
            //    ParticleSystem = new Graphics.Particles.ParticleSystem(500)
            //    {
            //        //Renderer = new Graphics.Particles.Renderers.MeshRenderer
            //        //{
            //        //    Mesh = Resources.Load<Mesh>("/models/sphere")
            //        //},
            //        Renderer = new Graphics.Particles.Renderers.BillboardRenderer(GraphicsBackend)
            //        {
            //            Material = Resources.Load<Material>("/materials/sphere")
            //        },
            //        Emitters = new List<Graphics.Particles.ParticleEmitter>()
            //        {
            //            new Graphics.Particles.ParticleEmitter
            //            {
            //                Generators = new List<Graphics.Particles.IParticleGenerator>()
            //                {
            //                    new Graphics.Particles.Generators.BasicTimeGenerator { MinTime = 0.1f, MaxTime = 0.3f },
            //                    new Graphics.Particles.Generators.BasicVelocityGenerator { MinStartVelocity = new Vector3(-1, -1, -1) * 10, MaxStartVelocity = new Vector3(1, 1, 1) * 10 },
            //                    new Graphics.Particles.Generators.BoxPositionGenerator() { Position = Vector3.Zero, MaxStartPosOffset = Vector3.Zero }
            //                }
            //            }
            //        },
            //        Updaters = new List<Graphics.Particles.IParticleUpdater>()
            //        {
            //            new Graphics.Particles.Updaters.BasicTimeUpdater(),
            //            new Graphics.Particles.Updaters.EulerUpdater() { GlobalAcceleration = new Vector3(0, 0, 0) },
            //            //new Graphics.Particles.Updaters.FloorUpdater() { BounceFactor = 0.5f, FloorPositionY = 0.0f }
            //        }
            //    }
            //});
            //GameWorld.Add(particles);

            Stage.ClearColor = new Vector4(1, 1, 1, 1) * 2;

            DeferredRenderer.Settings.ShadowQuality = Graphics.Deferred.ShadowQuality.High;
            DebugFlags |= Game.DebugFlags.RenderStats;
            DebugFlags |= Game.DebugFlags.ShadowMaps;

            //PostEffectManager.VisualizationMode = Graphics.Post.VisualizationMode.SSAO;
        }

        protected override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (InputManager.WasKeyPressed(Triton.Input.Key.Escape))
            {
                CursorVisible = !CursorVisible;
            }

            Light.Orientation = Quaternion.FromAxisAngle(Vector3.UnitX, _rotationX) * Quaternion.FromAxisAngle(Vector3.UnitY, _rotationY);
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);

            ImGui.SliderFloat("LightRotation X", ref _rotationX, -3.14f, 3.14f);
            ImGui.SliderFloat("LightRotation Y", ref _rotationY, -3.14f, 3.14f);
        }
    }
}
