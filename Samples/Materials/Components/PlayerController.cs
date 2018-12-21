using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Input;
using Triton.Physics.Components;

namespace Triton.Samples.Components
{
    public class PlayerController : BaseComponent
    {
        private const float MouseSensitivity = 0.0025f;

        private float _cameraYaw = 0;
        private float _cameraPitch = 0;

        private CharacterControllerComponent _characterController;
        private bool _wasMouseLeftPressed;

        private bool _wasSpaceDown = false;

        public override void OnActivate()
        {
            base.OnActivate();

            _characterController = Owner.GetComponent<CharacterControllerComponent>();
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
                movement = movement.Normalize();
            }

            var movementDir = Quaternion.FromAxisAngle(Vector3.UnitY, _cameraYaw);
            movement = Vector3.Transform(movement, movementDir);

            _cameraYaw += -Input.MouseDelta.X * MouseSensitivity;
            _cameraPitch += Input.MouseDelta.Y * MouseSensitivity;

            Camera.Orientation = Quaternion.Identity;
            Camera.Yaw(_cameraYaw);
            Camera.Pitch(_cameraPitch);

            _characterController.SetTargetVelocity(movement * 5.0f);
            if (Input.IsKeyDown(Key.Space) && !_wasSpaceDown)
            {
                _characterController.Jump();
                _wasSpaceDown = true;
            }
            else if (!Input.IsKeyDown(Key.Space))
            {
                _wasSpaceDown = false;
            }

            Camera.Position = Owner.Position + new Vector3(0, 0.7f, 0);

            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                _wasMouseLeftPressed = true;
            }
            else if (_wasMouseLeftPressed)
            {
                _wasMouseLeftPressed = false;

                var from = Camera.Position;
                var to = from + Vector3.Transform(new Vector3(0, 0, 100), Camera.Orientation);

                if (PhysicsWorld.Raycast(from, to, (component, _, __) => component.ColliderShape is Physics.Shapes.SphereColliderShape && component is RigidBodyComponent, out var hitComponent, out var hitNormal, out var hitFraction))
                {
                    var direction = to - from;
                    direction = direction.Normalize();
                    var rigidyBodyComponent = hitComponent as RigidBodyComponent;
                    rigidyBodyComponent.AddForce(direction * 10);
                }
            }
        }
    }
}
