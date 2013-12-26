using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content
{
	public class ContentData
	{
		public string OutputPath { get; set; }
		public DateTime LastCompilation { get; set; }
		public object Settings { get; set; }
	}
}
