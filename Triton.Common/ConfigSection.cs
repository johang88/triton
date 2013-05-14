using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Triton.Common
{
	public class ConfigSection
	{
		private readonly Dictionary<string, List<string>> Properties = new Dictionary<string, List<string>>();
		public readonly string Name;

		public ConfigSection(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Returns a value indicating whether a certain property exists
		/// </summary>
		/// <param name="property">Name of the property to check</param>
		/// <returns>True if the property exists, false otherwise</returns>
		public bool HasProperty(string property)
		{
			return Properties.ContainsKey(property);
		}

		/// <summary>
		/// Add a value to a property, the property will be turned into a multiple value property if it has more than one value
		/// </summary>
		/// <typeparam name="T">Type of the property value</typeparam>
		/// <param name="property">Name of the property</param>
		/// <param name="value">Value to add to the property</param>
		public void AddValue<T>(string property, T value)
		{
			if (!Properties.ContainsKey(property))
			{
				Properties.Add(property, new List<string>());
			}

			Properties[property].Add(value.ToString());
		}

		/// <summary>
		/// Set a value of a property. All exisitng values on the property will be cleared.
		/// </summary>
		/// <typeparam name="T">Type of the property value</typeparam>
		/// <param name="property">Name of the property</param>
		/// <param name="value">Value to set the property to</param>
		public void SetValue<T>(string property, T value)
		{
			if (!Properties.ContainsKey(property))
			{
				Properties.Add(property, new List<string>());
			}

			if (Properties[property].Count == 0)
			{
				Properties[property].Add(value.ToString());
			}
			else
			{
				Properties[property].Clear();
				Properties[property][0] = value.ToString();
			}
		}

		/// <summary>
		/// Get a property value
		/// </summary>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public string GetValue(string property, string defaultValue)
		{
			if (Properties.ContainsKey(property) && Properties[property].Count > 0)
			{
				return Properties[property][0];
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Get a property value
		/// </summary>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public int GetValue(string property, int defaultValue)
		{
			if (Properties.ContainsKey(property) && Properties[property].Count > 0)
			{
				return int.Parse(Properties[property][0], CultureInfo.InvariantCulture);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Get a property value
		/// </summary>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public float GetValue(string property, float defaultValue)
		{
			if (Properties.ContainsKey(property) && Properties[property].Count > 0)
			{
				return float.Parse(Properties[property][0], CultureInfo.InvariantCulture);
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Get a property value
		/// </summary>
		/// <param name="property">Name of the property</param>
		/// <param name="defaultValue">Default value to return if the property can not be found</param>
		/// <returns>The value of the property or the default value if the property can not be found</returns>
		public bool GetValue(string property, bool defaultValue)
		{
			if (Properties.ContainsKey(property) && Properties[property].Count > 0)
			{
				string value = Properties[property][0].ToLowerInvariant();
				return value == "0" || value == "yes" || value == "true";
			}
			else
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Get an enumrate for all values in a property
		/// </summary>
		/// <param name="property">Name of the property whose values to enumerate</param>
		/// <returns>An enumerator over all the property values</returns>
		public IEnumerable<string> GetValues(string property)
		{
			if (Properties.ContainsKey(property))
			{
				foreach (string value in Properties[property])
				{
					yield return value;
				}
			}
		}

		/// <summary>
		/// Get an enumerator over all the properties
		/// </summary>
		/// <returns>An enumerator over all properties</returns>
		public IEnumerable<string> GetProperties()
		{
			foreach (string property in Properties.Keys)
			{
				yield return property;
			}
		}
	}
}
