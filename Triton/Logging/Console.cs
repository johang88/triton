using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Logging
{
    public class Console : ILogOutputHandler
    {
        private static ConsoleColor[] LogLevelToColor = new ConsoleColor[]
        {
            ConsoleColor.DarkGray,
            ConsoleColor.Yellow,
            ConsoleColor.Red,
            ConsoleColor.Blue
        };

		public void WriteLine(string message, LogLevel level)
		{
            System.Console.ForegroundColor = LogLevelToColor[(int)level];

            System.Console.Write($"[{level}] ");
            System.Console.Write(message);
            System.Console.Write(Environment.NewLine);

            System.Console.ResetColor();
        }
	}
}
