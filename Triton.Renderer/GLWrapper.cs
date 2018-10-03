using OGL = OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public static class GLWrapper
	{
		private static bool _extDirectStateAccess = false;

		public static bool ExtDirectStateAccess { get { return _extDirectStateAccess; } }

		internal static void Initialize()
		{
			// Load extensions list
			var extensions = new Dictionary<string, bool>();
			int count = GL.GetInteger(GetPName.NumExtensions);
			for (var i = 0; i < count; i++)
			{
				string extension = GL.GetString(StringNameIndexed.Extensions, i);
				extensions.Add(extension, true);
			}

			// Check availability
			_extDirectStateAccess = extensions.ContainsKey("GL_EXT_direct_state_access");
		}

		public static void NamedBufferSubData(OGL.BufferTarget target, int handle, IntPtr offset, IntPtr size, IntPtr data)
		{
			if (_extDirectStateAccess)
			{
				GL.Ext.NamedBufferSubData(handle, offset, size, data);
			}
			else
			{
				int current = GL.GetInteger(target == OGL.BufferTarget.ArrayBuffer ? GetPName.ArrayBufferBinding : GetPName.ElementArrayBufferBinding);
				GL.BindBuffer(target, handle);
				GL.BufferSubData(target, offset, size, data);
				GL.BindBuffer(target, current);
			}
		}

		public static void NamedBufferData(OGL.BufferTarget target, int handle, IntPtr size, IntPtr data, BufferUsageHint usage)
		{
			if (_extDirectStateAccess)
			{
				GL.Ext.NamedBufferData(handle, size, data, (ExtDirectStateAccess)usage);
			}
			else
			{
				int current = GL.GetInteger(target == OGL.BufferTarget.ArrayBuffer ? GetPName.ArrayBufferBinding : GetPName.ElementArrayBufferBinding);
				GL.BindBuffer(target, handle);
				GL.BufferData(target, size, data, usage);
				GL.BindBuffer(target, current);
			}
		}

		public static void NamedBufferData<T>(OGL.BufferTarget target, int handle, IntPtr size, T[] data, BufferUsageHint usage)
			where T : struct
		{
			if (_extDirectStateAccess)
			{
				GL.Ext.NamedBufferData(handle, size, data, (ExtDirectStateAccess)usage);
			}
			else
			{
				int current = GL.GetInteger(target == OGL.BufferTarget.ArrayBuffer ? GetPName.ArrayBufferBinding : GetPName.ElementArrayBufferBinding);
				GL.BindBuffer(target, handle);
				GL.BufferData(target, size, data, usage);
				GL.BindBuffer(target, current);
			}
		}

		public static void NamedBufferStorage<T>(OGL.BufferTarget target, int handle, IntPtr size, T[] data, BufferStorageFlags flags)
			where T : struct
		{
			if (_extDirectStateAccess)
			{
				GL.Ext.NamedBufferStorage(handle, size, data, (int)flags);
			}
			else
			{
				int current = GL.GetInteger(target == OGL.BufferTarget.ArrayBuffer ? GetPName.ArrayBufferBinding : GetPName.ElementArrayBufferBinding);
				GL.BindBuffer(target, handle);
				GL.BufferStorage(target, size, data, flags);
				GL.BindBuffer(target, current);
			}
		}

		public static void BindMultiTexture(TextureUnit textureUnit, OGL.TextureTarget target, int handle)
		{
			if (_extDirectStateAccess)
			{
				GL.Ext.BindMultiTexture(textureUnit, target, handle);
			}
			else
			{
				GL.ActiveTexture(textureUnit);
				GL.BindTexture(target, handle);
			}
		}
	}
}
