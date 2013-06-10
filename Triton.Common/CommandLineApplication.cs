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
		private readonly CommandLineParser CommandLine;
		private List<Command> Commands = new List<Command>();
		private readonly string Usage;

		public CommandLineApplication(string[] parameters, string usage)
		{
			if (string.IsNullOrWhiteSpace(usage))
				throw new ArgumentNullException("usage");

			CommandLine = new CommandLineParser(parameters);
			Usage = usage;
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
			return !CommandLine.IsSet("help") && Commands.All(c => c.IsValid);
		}

		public void PrintUsage()
		{
			Console.WriteLine(Usage);
			Console.WriteLine("Available commands");
			foreach (var command in Commands)
			{
				Console.WriteLine("\t{0}: {1}", command.Name, command.Decription);
			}
		}

		class Command
		{
			public string Name { get; set; }
			public string Decription { get; set; }
			public bool IsValid { get; set; }
		}
	}
}
