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
	/// 
	/// Usage
	///  * Add commands
	///  * Validate
	///  * Print usage if not valid
	///  * Run application if valid
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

		/// <summary>
		/// Add a command to the command table
		/// </summary>
		/// <typeparam name="TValue">Type of the command</typeparam>
		/// <param name="name">Name of the command</param>
		/// <param name="description">A description that is shown when printing the command usage</param>
		/// <param name="required">Set to true if this command is required to be set, IsValid will fail if any required commands are not set</param>
		/// <param name="defaultValue"></param>
		/// <param name="setCommand">Callback function that is used to set the value of the command</param>
		/// <returns></returns>
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

		/// <summary>
		/// Check if the application is ready to run
		/// </summary>
		/// <returns>Returns true if all required commands are set</returns>
		public bool IsValid()
		{
			return !CommandLine.IsSet("help") && Commands.All(c => c.IsValid);
		}

		/// <summary>
		/// Print application usage information to standard output
		/// </summary>
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
