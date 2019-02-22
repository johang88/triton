using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Input;
using Triton.Physics.Components;
using Triton.Terrain;

namespace Triton.Samples.Components
{
    public class PlayerController : BaseComponent
    {
        private const float MouseSensitivity = 0.0025f;

        private CharacterControllerComponent _characterController;
        private bool _wasMouseLeftPressed;

        private bool _wasSpaceDown = false;

        public TerrainData Terrain { get; set; }

        //private KnightAnimator _animator;

        private ThirdPersonCamera _camera;

        public override void OnActivate()
        {
            base.OnActivate();

            _characterController = Owner.GetComponent<CharacterControllerComponent>();
            //_animator = Owner.Children.First().GetComponent<KnightAnimator>();
            _camera = Owner.GetComponent<ThirdPersonCamera>();
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (Input.UiHasFocus)
                return;

            // Player input
            var movement = Vector3.Zero;
            if (Input.IsKeyDown(Key.W))
                movement.Z = 1.0f;
            else if (Input.IsKeyDown(Key.S))
                movement.Z = -1.0f;

            if (Input.IsKeyDown(Key.A))
                movement.X = 1.0f;
            else if (Input.IsKeyDown(Key.D))
                movement.X = -1.0f;
            
            if (movement.LengthSquared > 0.0f)
            {
                var cameraYawOrientation = Quaternion.FromAxisAngle(Vector3.UnitY, _camera.YawAmount);
                var faceDirection = Vector3.Zero;

                faceDirection += movement.Z * cameraYawOrientation.ZAxis();
                faceDirection += movement.X * cameraYawOrientation.XAxis();
                faceDirection.Y = 0.0f;

                faceDirection = faceDirection.Normalize();

                var orientation = Vector3.GetRotationTo(Owner.Orientation.ZAxis(), faceDirection);
                Owner.Orientation *= orientation;

                _characterController.SetTargetVelocity(faceDirection * 3.5f * (Input.IsKeyDown(Key.ShiftLeft) ? 15.0f : 1.0f));

                //_animator.SetActiveAnimation("Soldier_walk");
            }
            else
            {
                //_animator.SetActiveAnimation("Idle");
                _characterController.SetTargetVelocity(Vector3.Zero);
            }

            _camera.Zoom(-Input.MouseWheelDelta);

            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                _camera.Yaw(-Input.MouseDelta.X * MouseSensitivity);
                _camera.Pitch(Input.MouseDelta.Y * MouseSensitivity);
            }

            _camera.TargetPosition = Owner.Position;
        }
    }
}
