using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Input;
using System.Drawing;
using System.Windows.Forms;

namespace Triton.Input
{
    public class InputManager
    {
		public Rectangle Bounds;
		private Point OldMousePosition;

		private Vector2 _MouseDelta;
		public Vector2 MouseDelta { get { return _MouseDelta; } }

		public bool LockMouse = true;

		public InputManager(Rectangle bounds)
		{
			Bounds = bounds;

			Cursor.Position = new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
			OldMousePosition = Cursor.Position;
		}

		public void Update()
		{
			_MouseDelta = new Vector2(Cursor.Position.X - OldMousePosition.X, Cursor.Position.Y - OldMousePosition.Y);

			if (LockMouse)
			{
				Cursor.Position = OldMousePosition = new Point(Bounds.Left + (Bounds.Width / 2), Bounds.Top + (Bounds.Height / 2));
			}
			else
			{
				OldMousePosition = Cursor.Position;
			}
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
