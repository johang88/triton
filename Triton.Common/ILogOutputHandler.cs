using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	public interface ILogOutputHandler
	{
		void WriteLine(string message, LogLevel level);
	}
}
