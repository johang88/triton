using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	public class Log
	{
		static readonly  object Lock = new object();
		static readonly List<ILogOutputHandler> OutputHandlers = new List<ILogOutputHandler>();

		public static void WriteLine(string message)
		{
			WriteLine(message, LogLevel.Default);
		}

		public static void WriteLine(string message, params object[] p)
		{
			WriteLine(string.Format(message, p), LogLevel.Default);
		}

		public static void WriteLine(string message, LogLevel level)
		{
			if (string.IsNullOrWhiteSpace(message))
				return; // Skip logging of empty messages

			lock (Lock)
			{
				foreach (ILogOutputHandler handler in OutputHandlers)
				{
					handler.WriteLine(message, level);
				}
			}
		}

		public static void AddOutputHandler(ILogOutputHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			OutputHandlers.Add(handler);
		}
	}
}
