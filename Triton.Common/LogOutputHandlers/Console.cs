using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common.LogOutputHandlers
{
	public class Console : ILogOutputHandler
	{
		public void WriteLine(string message, LogLevel level)
		{
			System.Console.WriteLine(string.Format("{0}", message));
		}
	}
}
