using ImGuiNET;
using SharpFileSystem;
using SharpFileSystem.FileSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton;
using Triton.Game;
using Triton.Graphics;
using Triton.Graphics.Components;
using Triton.Graphics.Post;
using Triton.Graphics.Resources;
using Triton.Graphics.SkeletalAnimation;

namespace ModelViewer
{
    class Application : Game
    {
        private string[] _models;
        private int _currentModelIndex = 0;

        private GameObject _modelGameObject;
        private MeshComponent _meshComponent;
        private SkinnedMeshComponent _skinnedMeshComponent;

        private string[] _animations = null;
        private int _currentAnimation = 0;
        private AnimationState _animationState = null;
        private bool _animationPlay = true;
        private bool _animationLoop = true;

        private float _cameraDistance = 0.0f;
        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;

        private float _zoomSpeed = 2.0f;
        private float _mouseSensivity = 0.05f;

        public Application() : base("Model Viewer")
        {
            RequestedWidth = 1920;
            RequestedHeight = 1070;
            ResolutionScale = 1.0f;
        }

        protected KeyValuePair<FileSystemPath, IFileSystem> Mount(string mountPoint, IFileSystem fileSystem)
        {
            return new KeyValuePair<FileSystemPath, IFileSystem>(FileSystemPath.Parse(mountPoint), fileSystem);
        }

        protected override SharpFileSystem.IFileSystem MountFileSystem()
        {
            return new FileSystemMounter(
                Mount("/tmp/", new PhysicalFileSystem("./tmp")),
                Mount("/", new MergedFileSystem(
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/core_data/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/samples_data/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/no_dist/")),
                    new ReadOnlyFileSystem(new PhysicalFileSystem("../Data/generated/"))
                    ))
                );
        }

        protected override void LoadResources()
        {
            base.LoadResources();

            PostEffectManager.HDRSettings.AutoKey = true;
            PostEffectManager.HDRSettings.TonemapOperator = TonemapOperator.ASEC;

            Stage.AmbientLight = new AmbientLight
            {
                Irradiance = Resources.Load<Texture>("/textures/sky_irradiance"),
                Specular = Resources.Load<Texture>("/textures/sky_specular")
            };

            _models = FileSystem.GetEntitties("/models/").ToArray();
            _currentModelIndex = -1;

            _modelGameObject = new GameObject();
            GameWorld.Add(_modelGameObject);

            _meshComponent = new MeshComponent();
            _skinnedMeshComponent = new SkinnedMeshComponent();

            //_modelGameObject.Components.Add(_meshComponent);
            //_modelGameObject.Components.Add(_skinnedMeshComponent);

            var light = new GameObject
            {
                Position = new Vector3(0, 0.4f, 0),
                Orientation = Quaternion.FromAxisAngle(Vector3.UnitX, 0.45f) * Quaternion.FromAxisAngle(Vector3.UnitY, -0.35f)
            };
            light.Components.Add(new LightComponent
            {
                Type = LighType.Directional,
                Intensity = 10,
                Color = new Vector3(1.1f, 1, 1),
                CastShadows = true
            });
            GameWorld.Add(light);
        }

        protected override void RenderUI(float deltaTime)
        {
            base.RenderUI(deltaTime);

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, RequestedHeight));
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0));
            ImGui.Begin("Model Viewer", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

            if (ImGui.Combo("Models", ref _currentModelIndex, _models, _models.Length))
            {
                var mesh = Resources.Load<Mesh>(_models[_currentModelIndex].Replace(".mesh", ""));
                if (mesh.Skeleton != null)
                {
                    _skinnedMeshComponent.Mesh = mesh;

                    if (_skinnedMeshComponent.Owner == null)
                    {
                        _modelGameObject.Components.Remove(_meshComponent);
                        _modelGameObject.Components.Add(_skinnedMeshComponent);
                    }
                    //_meshComponent.Mesh = null;

                    _animations = _skinnedMeshComponent.Skeleton.Animations.Select(x => x.Name).ToArray();
                    _currentAnimation = 0;
                    SetCurrentAnimation();
                }
                else
                {
                    _meshComponent.Mesh = mesh;

                    if (_meshComponent.Owner == null)
                    {
                        _modelGameObject.Components.Add(_meshComponent);
                        _modelGameObject.Components.Remove(_skinnedMeshComponent);
                    }

                    //_skinnedMeshComponent.Mesh = null;

                    _animations = null;
                    _animationState = null;
                }

                _modelGameObject.Position = Vector3.Zero;
                // TODO
                //_cameraDistance = mesh.BoundingSphereRadius * 3.0f;
                _rotationY = 0;
                _rotationX = 0;
            }

            if (_animations != null)
            {
                ImGui.Separator();
                if (ImGui.Combo("Animation", ref _currentAnimation, _animations, _animations.Length))
                {
                    SetCurrentAnimation();
                }

                if (_animationState != null)
                {
                    ImGui.Checkbox("Play", ref _animationPlay);

                    ImGui.Checkbox("Loop", ref _animationLoop);

                    _animationState.Loop = _animationLoop;
                    ImGui.SliderFloat("Time", ref _animationState.TimePosition, 0.0f, _animationState.Animation.Length, $"{_animationState.TimePosition:0.00}s", 1.0f);
                }
            }

            ImGui.Separator();

            ImGui.SliderFloat("Zoom speed", ref _zoomSpeed, 0.1f, 10.0f, "", 1f);
            ImGui.SliderFloat("Mouse sensitivity", ref _mouseSensivity, 0.01f, 1.0f, "", 1f);

            ImGui.End();
        }

        private void SetCurrentAnimation()
        {
            if (_animationState != null)
            {
                _animationState.Enabled = false;
            }

            if (_animations.Length > 0)
            {
                _animationState = _skinnedMeshComponent.GetAnimationState(_animations[_currentAnimation]);
                _animationState.Enabled = true;
                _animationState.TimePosition = 0.0f;
                _animationState.Weight = 1.0f;
                _animationState.Loop = _animationLoop;
            }
        }

        protected override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_animationState != null && _animationPlay)
            {
                _animationState.AddTime(frameTime);
            }

            if (!ImGui.GetIO().WantCaptureMouse)
            {
                _cameraDistance -= InputManager.MouseWheelDelta * _zoomSpeed;

                if (InputManager.IsMouseButtonDown(Triton.Input.MouseButton.Right))
                {
                    _rotationY += InputManager.MouseDelta.X * _mouseSensivity;
                    _rotationX += -InputManager.MouseDelta.Y * _mouseSensivity;
                }

                if (InputManager.IsMouseButtonDown(Triton.Input.MouseButton.Middle))
                {
                    _modelGameObject.Position.X -= InputManager.MouseDelta.X * _mouseSensivity;
                    _modelGameObject.Position.Y -= -InputManager.MouseDelta.Y * _mouseSensivity;
                }
            }

            var orientation = Quaternion.FromAxisAngle(Vector3.UnitY, Triton.Math.Util.DegreesToRadians(_rotationY)) * 2.0f;
            _modelGameObject.Orientation = orientation * Quaternion.FromAxisAngle(Vector3.UnitX, Triton.Math.Util.DegreesToRadians(_rotationX)) * 2.0f;

            Camera.Position = new Vector3(0, 0, -_cameraDistance);
        }
    }
}
