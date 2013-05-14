using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Triton.Common
{
	public class SettingsSerializer
	{
		public void Serialize(System.IO.Stream stream, Type settingsType)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			StringBuilder sb = new StringBuilder();
			SaveSettingsFromClass(sb, settingsType, "");

			using (var writer = new StreamWriter(stream))
			{
				writer.Write(sb.ToString());
			}
		}

		public void Deserialize(System.IO.Stream stream, Type settingsType)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			ConfigFile configFile = ConfigFile.Parse(stream, new char[] { '\t', ':', '=' }, true);

			LoadSettingsFromClass(configFile, settingsType, "");
		}

		void LoadSettingsFromClass(ConfigFile configFile, Type settingsClass, string sectionName)
		{
			foreach (FieldInfo fieldInfo in settingsClass.GetFields())
			{
				if (fieldInfo.FieldType == typeof(Dictionary<string, string>))
				{
					Dictionary<string, string> dict = new Dictionary<string, string>();
					var section = configFile.GetSection(sectionName);

					foreach (var property in section.GetProperties())
					{
						var value = section.GetValue(property, "");
						if (dict.ContainsKey(property))
						{
							dict.Add(property, value);
						}
						else
						{
							dict[property] = value;
						}
					}

					fieldInfo.SetValue(null, dict);
				}
				else if (configFile.HasProperty(sectionName, fieldInfo.Name))
				{
					string stringValue = configFile.GetValue(sectionName, fieldInfo.Name, "");
					object value = StringConverter.Parse(stringValue, fieldInfo.FieldType);

					fieldInfo.SetValue(null, value);
				}
			}

			foreach (Type nestedType in settingsClass.GetNestedTypes())
			{
				string newSectionName = sectionName;
				if (newSectionName.Length > 0)
				{
					newSectionName += ".";
				}
				newSectionName += nestedType.Name;

				LoadSettingsFromClass(configFile, nestedType, newSectionName);
			}
		}

		void SaveSettingsFromClass(StringBuilder sb, Type settingsClass, string sectionName)
		{
			if (sectionName.Length > 0)
			{
				sb.AppendLine("[" + sectionName + "]");
			}

			foreach (FieldInfo fieldInfo in settingsClass.GetFields())
			{
				if (fieldInfo.FieldType == typeof(Dictionary<string, string>))
				{
					Dictionary<string, string> dict = (Dictionary<string, string>)fieldInfo.GetValue(null);
					foreach (KeyValuePair<string, string> pair in dict)
					{
						sb.AppendLine(pair.Key + " = " + pair.Value);
					}
				}
				else
				{
					object value = fieldInfo.GetValue(null);
					string stringValue = StringConverter.ToString(value, fieldInfo.FieldType);

					sb.AppendLine(fieldInfo.Name + " = " + stringValue);
				}
			}

			foreach (Type nestedType in settingsClass.GetNestedTypes())
			{
				if (sb.Length > 0)
				{
					sb.AppendLine();
				}

				string newSectionName = sectionName;
				if (newSectionName.Length > 0)
				{
					newSectionName += ".";
				}
				newSectionName += nestedType.Name;

				SaveSettingsFromClass(sb, nestedType, newSectionName);
			}
		}
	}
}
