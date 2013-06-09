using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	public class CommandLineParser
	{
		private readonly Dictionary<string, string> Parameters;
		private readonly string[] FalseValues = new string[] { "false", "no", "0" };

		public CommandLineParser(string[] parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");

			Parameters = new Dictionary<string, string>();

			foreach (var parameter in parameters)
			{
				var parameterName = parameter; // We need a local copy as we modify the iterator object
				var parameterValue = "";

				// Better safe than sorry
				if (parameterName.Length == 0)
					continue;

				// strip initial from param name '-'
				if (parameterName[0] == '-')
					parameterName = parameterName.Substring(1);

				// Check if a value is assigned to the parameter
				if (parameterName.Contains('='))
				{
					var split = parameterName.Split('=');
					if (split.Length != 2)
						continue;

					parameterName = split[0];
					parameterValue = split[1];
				}
				else
				{
					// Parameters without values are assumed to be boolean
					parameterValue = "1";
				}

				// We strip any boolean values that parse as "false", this way we can later check boolean values with Parameters.ContainsKey(name)
				if (FalseValues.Contains(parameterValue.ToLowerInvariant()))
					continue;

				Parameters.Add(parameterName, parameterValue);
			}
		}

		public bool IsSet(string name)
		{
			return Parameters.ContainsKey(name);
		}

		public TValue Get<TValue>(string name)
		{
			if (!IsSet(name))
				return default(TValue);

			return StringConverter.Parse<TValue>(Parameters[name]);
		}
	}
}
