using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
	/// A base command line application class
	/// Provides support for settings to bet set through the command line, with both required and non required values
	/// A help text of supported commands is printed if not all required commands are set
	/// </summary>
	public class CommandLineApplication
	{
		public readonly CommandLineParser CommandLine;
		private List<Command> Commands = new List<Command>();

		public CommandLineApplication(string[] parameters)
		{
			CommandLine = new CommandLineParser(parameters);
		}

		public CommandLineApplication AddCommand<TValue>(string name, string description, bool required, TValue defaultValue, Action<TValue> setCommand)
		{
			Commands.Add(new Command
			{
				Name = name,
				Decription = description,
				IsValid = !required || CommandLine.IsSet(name)
			});

			setCommand(CommandLine.Get(name, defaultValue));

			return this;
		}

		public bool IsValid()
		{
			return Commands.All(c => c.IsValid);
		}

		class Command
		{
			public string Name { get; set; }
			public string Decription { get; set; }
			public bool IsValid { get; set; }
		}
	}
}
