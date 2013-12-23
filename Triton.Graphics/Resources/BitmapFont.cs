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

			foreach (var c in text)
			{
				if (c == '\t')
				{
					position.X += Glyphs.First().Value.XAdvance * 4;
				}
				else if (c == '\n')
				{
					position.X = startX;
					position.Y -= LineHeight;
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
