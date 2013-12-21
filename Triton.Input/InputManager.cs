using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Input;
using System.Drawing;

namespace Triton.Input
{
    public class InputManager
    {
		public Rectangle Bounds;
		private Vector2 OldMousePosition;

		private Vector2 _MouseDelta;
		public Vector2 MouseDelta { get { return _MouseDelta; } }

		public InputManager(Rectangle bounds)
		{
			Bounds = bounds;

			Mouse.SetPosition(0, 0);
			OldMousePosition = new Vector2(0, 0);
		}

		public void Update()
		{
			var state = Mouse.GetState();

			_MouseDelta = new Vector2(state.X - OldMousePosition.X, state.Y - OldMousePosition.Y);

			OldMousePosition.X = state.X;
			OldMousePosition.Y = state.Y;
		}

		public bool IsKeyDown(Key key)
		{
			return Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)(int)key);
		}

		public bool IsMouseButtonDown(MouseButton button)
		{
			return Mouse.GetState().IsButtonDown((OpenTK.Input.MouseButton)(int)button);
		}
    }
}
