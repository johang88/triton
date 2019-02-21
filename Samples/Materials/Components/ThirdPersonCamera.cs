using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Game.World.Components;
using static System.Math;

namespace Triton.Samples.Components
{
    public class ThirdPersonCamera : BaseComponent
    {
        [DataMember] public float Height { get; set; } = 0.25f;

        [DataMember] public float MinDistance { get; set; } = 1.0f;
        [DataMember] public float MaxDistance { get; set; } = 8.0f;
        [DataMember] public float ZoomSpeed { get; set; } = 5.0f;

        [DataMember] public float MinPitch { get; set; } = Math.Util.DegreesToRadians(-10);
        [DataMember] public float MaxPitch { get; set; } = Math.Util.DegreesToRadians(44);

        private float _previousZoomDistance = 0.0f;
        private float _zoomDistance = 0.0f;
        private float _zoomAlpha = 1.0f;

        public float YawAmount { get; private set; } = 0.0f;
        public float PitchAmount { get; private set; } = 0.0f;

        public Vector3 TargetPosition { get; set; }

        public override void OnActivate()
        {
            base.OnActivate();

            _zoomDistance = _previousZoomDistance = (MaxDistance - MinDistance) / 2.0f;
            _zoomAlpha = 1.0f;

            YawAmount = 0.0f;
            PitchAmount = Math.Util.DegreesToRadians((MaxPitch - MinPitch) / 2.0f);
        }

        public void Zoom(float amount)
        {
            if (amount != 0.0f)
            {
                _previousZoomDistance = _zoomDistance;
                _zoomAlpha = 0.0f;

                _zoomDistance = Min(MaxDistance, Max(MinDistance, _zoomDistance + amount));
            }
        }

        public void Pitch(float amount)
             => PitchAmount = Min(MaxPitch, Max(MinPitch, PitchAmount + amount));

        public void Yaw(float amount)
            => YawAmount += amount;

        public override void Update(float dt)
        {
            base.Update(dt);

            _zoomAlpha += ZoomSpeed * dt;
            _zoomAlpha = Min(1.0f, Max(0.0f, _zoomAlpha));

            var targetPosition = TargetPosition;
            targetPosition.Y += Height;

            var orientation = Quaternion.FromAxisAngle(Vector3.UnitY, YawAmount);
            orientation *= Quaternion.FromAxisAngle(Vector3.UnitX, PitchAmount);

            var position = Vector3.Transform(new Vector3(0, 0, Math.Util.Lerp(_previousZoomDistance, _zoomDistance, _zoomAlpha)), orientation);
            position = targetPosition - position;

            Camera.Position = position;
            Camera.Orientation = orientation;
        }
    }
}
