using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World;
using Triton.Game.World.Components;
using Triton.Input;

namespace Triton.Samples.Components
{
    public class PlayerController : Component
    {
        private const float MouseSensitivity = 0.0025f;

        private float _cameraYaw = 0;
        private float _cameraPitch = 0;

        private CharacterController _characterController;
        private bool _wasMouseLeftPressed;

        public override void OnActivate()
        {
            base.OnActivate();

            _characterController = Owner.GetComponent<CharacterController>();
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

            _characterController.Move(movement, Input.IsKeyDown(Key.Space));
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

                if (World.PhysicsWorld.Raycast(from, to, (body, _, __) => body.CollisionLayer == 2, out var hitBody, out var hitNormal, out var hitFraction))
                {
                    hitBody.AddForce(hitNormal * 10);
                }
            }
        }
    }
}
