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

		private bool[] PreviousState = new bool[(int)Key.LastKey];
		private bool[] WasPressedState = new bool[(int)Key.LastKey];

        public bool UiHasFocus { get; set; }

		public InputManager(Rectangle bounds)
		{
			Bounds = bounds;

			Mouse.SetPosition(0, 0);
			OldMousePosition = new Vector2(0, 0);

			for (int i = 0; i < PreviousState.Length; i++)
			{
				PreviousState[i] = false;
				WasPressedState[i] = false;
			}
		}

		public void Update()
		{
			var state = Mouse.GetState();

			_MouseDelta = new Vector2(state.X - OldMousePosition.X, state.Y - OldMousePosition.Y);

			OldMousePosition.X = state.X;
			OldMousePosition.Y = state.Y;

			for (int i = 0; i < PreviousState.Length; i++)
			{
				if (PreviousState[i] && !Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)i))
				{
					WasPressedState[i] = true;
					PreviousState[i] = false;
				}
				else
				{
					WasPressedState[i] = false;
					PreviousState[i] = Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)i);
				}
			}
		}

		public bool IsKeyDown(Key key)
		{
			return Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)(int)key);
		}

		public bool WasKeyPressed(Key key)
		{
			return WasPressedState[(int)key];
		}

		public bool IsMouseButtonDown(MouseButton button)
		{
			return Mouse.GetState().IsButtonDown((OpenTK.Input.MouseButton)(int)button);
		}
    }
}
