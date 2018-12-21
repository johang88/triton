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
		private Vector2 _oldMousePosition;

        private float _oldWheelPosition;
        public float MouseWheelDelta { get;private set; }

        public Vector2 MouseDelta { get; private set; }

        private bool[] _previousState = new bool[(int)Key.LastKey];
		private bool[] _wasPressedState = new bool[(int)Key.LastKey];

        public bool UiHasFocus { get; set; }

		public InputManager(Rectangle bounds)
		{
			Bounds = bounds;

			Mouse.SetPosition(0, 0);
			_oldMousePosition = new Vector2(0, 0);

            _oldWheelPosition = Mouse.GetState().WheelPrecise;

            for (int i = 0; i < _previousState.Length; i++)
			{
				_previousState[i] = false;
				_wasPressedState[i] = false;
			}
		}

		public void Update()
		{
			var state = Mouse.GetState();

            MouseWheelDelta = state.WheelPrecise - _oldWheelPosition;
            _oldWheelPosition = state.WheelPrecise;

            MouseDelta = new Vector2(state.X - _oldMousePosition.X, state.Y - _oldMousePosition.Y);

			_oldMousePosition.X = state.X;
			_oldMousePosition.Y = state.Y;

			for (int i = 0; i < _previousState.Length; i++)
			{
				if (_previousState[i] && !Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)i))
				{
					_wasPressedState[i] = true;
					_previousState[i] = false;
				}
				else
				{
					_wasPressedState[i] = false;
					_previousState[i] = Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)i);
				}
			}
		}

		public bool IsKeyDown(Key key)
		{
			return Keyboard.GetState().IsKeyDown((OpenTK.Input.Key)(int)key);
		}

		public bool WasKeyPressed(Key key)
		{
			return _wasPressedState[(int)key];
		}

		public bool IsMouseButtonDown(MouseButton button)
		{
			return Mouse.GetState().IsButtonDown((OpenTK.Input.MouseButton)(int)button);
		}
    }
}
