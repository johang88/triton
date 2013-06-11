using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Common
{
	/// <summary>
	/// A config file is a collection of sections that in turn stores a collection of key-value pairs.
	/// 
	/// The ConfigFile class provides methods for parsing and serializing config files.
	/// </summary>
	public class ConfigFile
	{
		private readonly Dictionary<string, ConfigSection> Sections = new Dictionary<string, ConfigSection>();

		/// <summary>
		/// Get a property value in a specific section, the section will be created if it does not exist
		/// </summary>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public string GetValue(string section, string property, string defaultValue)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			return Sections[section].GetValue(property, defaultValue);
		}

		public bool HasProperty(string section, string property)
		{
			return Sections.ContainsKey(section) && Sections[section].HasProperty(property);
		}

		/// <summary>
		/// Get a property value in a specific section, the section will be created if it does not exist
		/// </summary>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public int GetValue(string section, string property, int defaultValue)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			return Sections[section].GetValue(property, defaultValue);
		}

		/// <summary>
		/// Get a property value in a specific section, the section will be created if it does not exist
		/// </summary>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public float GetValue(string section, string property, float defaultValue)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			return Sections[section].GetValue(property, defaultValue);
		}

		/// <summary>
		/// Get a property value in a specific section, the section will be created if it does not exist
		/// </summary>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public bool GetValue(string section, string property, bool defaultValue)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			return Sections[section].GetValue(property, defaultValue);
		}

		/// <summary>
		/// Add a value to a property in a specific section, the property will be turned into a multiple value property if it has more than one value
		/// </summary>
		/// <typeparam name="T">Type of the property value</typeparam>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="value">Value to add to the property</param>
		public void AddValue<T>(string section, string property, T value)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			Sections[section].AddValue(property, value);
		}

		/// <summary>
		/// Set a value of a property in a specific section. All exisitng values on the property will be cleared.
		/// </summary>
		/// <typeparam name="T">Type of the property value</typeparam>
		/// <param name="section">Name of the section</param>
		/// <param name="property">Name of the property</param>
		/// <param name="value">Value to set the property to</param>
		public void SetValue<T>(string section, string property, T value)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			Sections[section].SetValue(property, value);
		}

		/// <summary>
		/// Add a section
		/// </summary>
		/// <param name="section">Name of the section</param>
		public void AddSection(string section)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}
		}

		/// <summary>
		/// Get a section by name, the section will be created if it does not exist
		/// </summary>
		/// <param name="section">Name of the section</param>
		/// <returns>The named section</returns>
		public ConfigSection GetSection(string section)
		{
			if (!Sections.ContainsKey(section))
			{
				Sections.Add(section, new ConfigSection(section));
			}

			return Sections[section];
		}

		/// <summary>
		/// Get an enumerator for all sections
		/// </summary>
		/// <returns>An enumerator for all sections</returns>
		public IEnumerable<ConfigSection> GetSections()
		{
			foreach (ConfigSection section in Sections.Values)
			{
				yield return section;
			}
		}

		/// <summary>
		/// Parse a config file from a stream
		/// </summary>
		/// <param name="stream">Stream to parse</param>
		/// <returns>The parsed config file</returns>
		public static ConfigFile Parse(Stream stream)
		{
			return Parse(stream, new char[] { '=' }, true);
		}

		/// <summary>
		/// Parse a config file from a stream
		/// </summary>
		/// <param name="stream">Stream to parse</param>
		/// <param name="separators">An array of characters that seperates key from value</param>
		/// <param name="trimWhiteSpace">Whitespace will be trimmed if true</param>
		/// <returns>The parsed config file</returns>
		public static ConfigFile Parse(Stream stream, char[] separators, bool trimWhiteSpace)
		{
			ConfigFile configFile = new ConfigFile();

			using (StreamReader reader = new StreamReader(stream))
			{
				string currentSection = "";

				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();

					if (line.Length > 0 && line[0] != '#')
					{
						if (line[0] == '[' && line[line.Length - 1] == ']')
						{
							currentSection = line.Substring(1, line.Length - 2);
							configFile.AddSection(currentSection);
						}
						else
						{
							int separatorPosition = line.IndexOfAny(separators);
							if (separatorPosition >= 0)
							{
								string propertyName = line.Substring(0, separatorPosition);
								if (line.Length > separatorPosition + 1)
								{
									string properyValue = line.Substring(separatorPosition + 1);

									if (trimWhiteSpace)
									{
										propertyName = propertyName.Trim();
										properyValue = properyValue.Trim();
									}

									configFile.AddValue(currentSection, propertyName, properyValue);
								}
							}
						}
					}
				}
			}

			return configFile;
		}
	}
}
