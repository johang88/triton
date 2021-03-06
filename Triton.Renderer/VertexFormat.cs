﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Renderer
{
	public class VertexFormat
	{
		public readonly int Size;
		public readonly VertexFormatElement[] Elements;

		public VertexFormat(VertexFormatElement[] elements)
		{
			if (elements == null)
				throw new ArgumentNullException("elements");
			if (elements.Length == 0)
				throw new ArgumentException("empty vertex format elements array");

			Elements = elements;

			foreach (var element in Elements)
			{
                if (element.Type == VertexPointerType.Float || element.Type == VertexPointerType.Int)
                    Size += 4 * element.Count;
                else if (element.Type == VertexPointerType.Short)
                    Size += 2 * element.Count;
                else if (element.Type == VertexPointerType.Byte || element.Type == VertexPointerType.UnsignedByte)
                    Size += 1 * element.Count;
            }
		}

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var element in Elements)
            {
                sb.AppendLine($"{element.Semantic} {element.Type} {element.Count} {element.Offset} {element.Divisor} {element.Normalized}");
            }

            return sb.ToString();
        }
    }
}
