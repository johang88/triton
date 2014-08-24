using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics
{
	public class Camera
	{
		public bool Orthographic = false;

		public Matrix4? CustomViewMatrix = null;

		public float NearClipDistance;
		public float FarClipDistance;

		public float Fov;

		public Vector2 Viewport { get; private set; }

		Vector3 Look = Vector3.UnitZ;
		Vector3 Up = Vector3.UnitY;

		public Quaternion Orientation;
		public Vector3 Position;

		private BoundingFrustum Frustum = new BoundingFrustum(Matrix4.Identity);

		public Camera(Vector2 viewport)
		{
			Viewport = viewport;
			Fov = 1.04719755f;
			Orientation = Quaternion.Identity;

			NearClipDistance = 0.1f;
			FarClipDistance = 512.0f;
		}

		public void GetUpVector(out Vector3 up)
		{
			Orientation.Normalize();
			Vector3.Transform(ref Up, ref Orientation, out up);
		}

		public void GetViewMatrix(out Matrix4 viewMatrix, bool noTranslation = false)
		{
			if (CustomViewMatrix != null)
			{
				viewMatrix = CustomViewMatrix.Value;
				return;
			}

			Orientation.Normalize();

			Vector3 look;
			Vector3.Transform(ref Look, ref Orientation, out look);

			Vector3 up;
			Vector3.Transform(ref Up, ref Orientation, out up);

			Vector3 position = noTranslation ? Vector3.Zero : Position;

			viewMatrix = Matrix4.LookAt(position, position + look, up);
		}

		public Vector3 GetScreenSpacePosition(Vector3 src)
		{
			Matrix4 viewMatrix;
			GetViewMatrix(out viewMatrix);

			Vector3 worldViewPosition;
			Vector3.Transform(ref src, ref viewMatrix, out worldViewPosition);

			Matrix4 projectionMatrix;
			GetProjectionMatrix(out projectionMatrix);

			Vector3 hcsPosition;
			Vector3.TransformPerspective(ref worldViewPosition, ref projectionMatrix, out hcsPosition);

			return new Vector3((0.5f + (0.5f * hcsPosition.X)) * Viewport.X, (0.5f + (0.5f * -hcsPosition.Y)) * Viewport.Y, hcsPosition.Z);
		}

		public void GetProjectionMatrix(out Matrix4 projectionMatrix)
		{
			if (Orthographic)
				Matrix4.CreateOrthographicOffCenter(0.0f, Viewport.X, Viewport.Y, 0.0f, NearClipDistance, FarClipDistance, out projectionMatrix);
			else
				Matrix4.CreatePerspectiveFieldOfView(Fov, Viewport.X / Viewport.Y, NearClipDistance, FarClipDistance, out projectionMatrix);
		}

		public void Yaw(float amount)
		{
			Orientation *= Quaternion.FromAxisAngle(Vector3.UnitY, amount);
		}

		public void Pitch(float amount)
		{
			Orientation *= Quaternion.FromAxisAngle(Vector3.UnitX, amount);
		}

		public void Roll(float amount)
		{
			Orientation *= Quaternion.FromAxisAngle(Vector3.UnitZ, amount);
		}

		public void Move(ref Vector3 amount)
		{
			Position += amount;
		}

		public void Move(float x, float y, float z)
		{
			Position.X += x;
			Position.Y += y;
			Position.Z += z;
		}

		public BoundingFrustum GetFrustum()
		{
			Matrix4 view, projection;

			GetViewMatrix(out view);
			GetProjectionMatrix(out projection);

			Frustum.Matrix = view * projection;

			return Frustum;
		}
	}
}
