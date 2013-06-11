using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
	/// Interface for log output handling.
	/// Log output handlers are use to write log messages to arbitrary locations.
	/// </summary>
	public interface ILogOutputHandler
	{
		void WriteLine(string message, LogLevel level);
	}
}
