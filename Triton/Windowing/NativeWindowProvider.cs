using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Windowing
{
	public sealed class NativeWindowProvider : IWindowProvider, IDisposable
	{
		private readonly OpenTK.INativeWindow Window;

		public NativeWindowProvider(string name, int width, int height)
		{
			var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 0, 0);

			Window = new NativeWindow(width, height, name, GameWindowFlags.Default, graphicsMode, DisplayDevice.Default);
			Window.Visible = true;
		}

		public void Dispose()
		{
			Window.Dispose();
		}

		public void ProcessEvents()
		{
			Window.ProcessEvents();
		}

		public OpenTK.Platform.IWindowInfo WindowInfo
		{
			get { return Window.WindowInfo; }
		}

		public IntPtr Handle
		{
			get { return Window.WindowInfo.Handle; }
		}

		public System.Drawing.Rectangle Bounds
		{
			get { return Window.Bounds; }
		}

		public int Width
		{
			get { return Window.Width; }
		}

		public int Height
		{
			get { return Window.Height; }
		}

		public bool CanResize
		{
			get { return true; }
		}

		public void Resize(int width, int height)
		{
			Window.Width = width;
			Window.Height = height;

			if (OnResize != null)
				OnResize(Window.Width, Window.Height);
		}

		public WindowResizeDelegate OnResize { get; set; }

		public bool Exists
		{
			get { return Window.Exists; }
		}

		public bool CursorVisible
		{
			get
			{
				return Window.Visible;
			}
			set
			{
				Window.Visible = value;
			}
		}
	}
}
