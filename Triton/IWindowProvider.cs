using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
	public delegate void WindowResizeDelegate(int width, int height);

	/// <summary>
	/// Supplies all neccecary window info
	/// </summary>
	public interface IWindowProvider
	{
		// Core implementation details
		OpenTK.Platform.IWindowInfo WindowInfo { get; }
		IntPtr Handle { get; }

		// Bounds info
		System.Drawing.Rectangle Bounds { get; }
		int Width { get; }
		int Height { get; }

		// Resizing
		bool CanResize { get; }
		void Resize(float width, float height);

		WindowResizeDelegate OnResize { get; set; }

		// Misc
		bool Exists { get; }
		bool CursorVisible { get; set; }
	}
}
