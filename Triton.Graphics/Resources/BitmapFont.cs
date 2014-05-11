using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Graphics.Resources
{
	public class BitmapFont : Triton.Common.Resource
	{
		internal List<Texture> Textures = new List<Texture>();
		internal Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();
		public float LineHeight;
		public Vector2 Scale = new Vector2();

		private static Dictionary<string, Vector4> Colors = new Dictionary<string, Vector4>();

		static BitmapFont()
		{
			Colors.Add("red", new Vector4(1, 0, 0, 1));
			Colors.Add("blue", new Vector4(0, 0, 1, 1));
			Colors.Add("green", new Vector4(0, 1, 0, 1));
			Colors.Add("white", new Vector4(1, 1, 1, 1));
			Colors.Add("black", new Vector4(0, 0, 0, 1));
		}

		public BitmapFont(string name, string parameters)
			: base(name, parameters)
		{
		}

		public void DrawText(SpriteBatch sprite, Vector2 position, Vector4 color, string text, params object[] parameters)
		{
			if (parameters.Length > 0)
			{
				text = string.Format(text, parameters);
			}

			var startX = position.X;

			for (var i = 0; i < text.Length; i++)
			{
				var c = text[i];

				switch (c)
				{
					case '\t':
						position.X += Glyphs.First().Value.XAdvance * 4;
						break;
					case '\n':
						position.X = startX;
						position.Y += LineHeight;
						break;
					case '[':
						if (text.Length < i + 2 || text[i + 1] != '#')
							break;

						i += 2;
						var colorCode = "";

						while (text[i] != ']' && text.Length > i + 1)
						{
							colorCode += text[i++];
						}

						if (Colors.ContainsKey(colorCode))
						{
							color = Colors[colorCode];
						}
						else
						{
							var bytes = Common.Utility.Hex.StringToByteArray(colorCode);

							color = new Vector4(
								(float)bytes[0] / 255.0f,
								(float)bytes[1] / 255.0f,
								(float)bytes[2] / 255.0f,
								1
								);
						}

						c = text[++i];

						break;
				}

				if (!Glyphs.ContainsKey(c))
					continue;

				var glyph = Glyphs[c];

				sprite.RenderQuad(Textures[glyph.Page], position + glyph.Offset, glyph.Size, glyph.UvPosition, glyph.UvSize, color);
				position.X += glyph.XAdvance;
			}
		}

		public class Glyph
		{
			public Vector2 UvPosition;
			public Vector2 UvSize;
			public Vector2 Size;
			public Vector2 Offset;
			public float XAdvance;
			public int Page;
		}
	}
}
