using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;


namespace Triton.Common
{
	/// <summary>
	/// Utility class for parsing strings and serializing values in a culture invariant format
	/// </summary>
	public static class StringConverter
	{
		public static TValue Parse<TValue>(string stringValue)
		{
			return (TValue)Parse(stringValue, typeof(TValue));
		}

		public static object Parse(string stringValue, Type targetType)
		{
			if (targetType == typeof(string))
			{
				return stringValue;
			}

			// Check if we want to parse a HashedString or a sub class of HashedString
			var type = targetType;
			while (type != typeof(object))
			{
				if (type == typeof(HashedString))
				{
					return ParseHashedString(stringValue, targetType);
				}
				type = type.BaseType;
			}

			if (targetType == typeof(Vector2))
			{
				return ParseVector2(stringValue);
			}

			if (targetType == typeof(Vector3))
			{
				return ParseVector3(stringValue);
			}

			if (targetType == typeof(Vector4))
			{
				return ParseVector4(stringValue);
			}

			if (targetType == typeof(Quaternion))
			{
				return ParseQuaternion(stringValue);
			}

			if (targetType == typeof(bool))
			{
				return ParseBool(stringValue);
			}

			if (targetType.IsEnum)
			{
				// Only try non case sensitive parse if necessary
				if (Enum.GetNames(targetType).Contains(stringValue))
				{
					return Enum.Parse(targetType, stringValue);
				}
				else
				{
					return Enum.Parse(targetType, stringValue, true);
				}
			}

			if (targetType.GetInterfaces().Contains(typeof(IList)) && targetType.IsGenericType)
			{
				ConstructorInfo ci = targetType.GetConstructor(new Type[] { });
				IList list = (IList)ci.Invoke(null);
				ParseList(stringValue, list, targetType.GetGenericArguments()[0]);
				return list;
			}

			if (targetType == typeof(bool))
			{
				stringValue = stringValue.ToLowerInvariant();
				return stringValue == "0" || stringValue == "yes" || stringValue == "true";
			}

			MethodInfo parseMethod = typeof(StringConverter).GetMethod("Parse" + targetType.Name, new Type[] { typeof(string) });
			if (parseMethod != null)
			{
				return parseMethod.Invoke(null, new object[] { stringValue });
			}

			parseMethod = targetType.GetMethod("Parse", new Type[] { typeof(string), typeof(IFormatProvider) });
			if (parseMethod != null)
			{
				return parseMethod.Invoke(null, new object[] { stringValue, CultureInfo.InvariantCulture });
			}

			parseMethod = targetType.GetMethod("Parse");
			if (parseMethod != null)
			{
				return parseMethod.Invoke(null, new object[] { stringValue });
			}

			throw new InvalidOperationException("No parse method found for type " + targetType.Name);
		}

		static object ParseHashedString(string stringValue, Type targetType)
		{
			var ctor = targetType.GetConstructor(new Type[] { typeof(string) });
			return ctor.Invoke(new object[] { stringValue });
		}

		public static void ParseList(string stringValue, IList list, Type targetType)
		{
			string[] split = stringValue.Split(',');
			foreach (string value in split)
			{
				string trimValue = value.Trim();
				list.Add(Parse(trimValue, targetType));
			}
		}

		public static string ListToString(IList list)
		{
			StringBuilder sb = new StringBuilder();
			Type targetType = list.GetType().GetGenericArguments()[0];

			bool first = true;
			foreach (object o in list)
			{
				if (!first)
				{
					sb.Append(",");
				}
				else
				{
					first = false;
				}

				sb.Append(ToString(o, targetType));
			}

			return sb.ToString();
		}

		public static string ToString<TType>(TType o)
		{
			return ToString(o, typeof(TType));
		}

		public static string ToString(object o, Type targetType)
		{
			if (o is string)
			{
				return (string)o;
			}

			if (o is Vector3)
			{
				var v = (Vector3)o;
				return ToString(v.X) + " " + ToString(v.Y) + " " + ToString(v.Z);
			}

			if (o is Vector4)
			{
				var v = (Vector4)o;
				return ToString(v.X) + " " + ToString(v.Y) + " " + ToString(v.Z) + " " + v.W;
			}

			if (o is Quaternion)
			{
				var v = (Quaternion)o;
				return ToString(v.X) + " " + ToString(v.Y) + " " + ToString(v.Z) + " " + v.W;
			}

			if (o is bool)
			{
				return (bool)o ? "true" : "false";
			}

			if (targetType.GetInterfaces().Contains(typeof(IList)) && targetType.IsGenericType)
			{
				return ListToString((IList)o);
			}

			if (o is HashedString)
			{
				var hash = o as HashedString;
				return HashedStringTable.GetString(hash);
			}

			MethodInfo toStringMethod = targetType.GetMethod("ToString", new Type[] { typeof(IFormatProvider) });
			if (toStringMethod != null)
			{
				return (string)toStringMethod.Invoke(o, new object[] { CultureInfo.InvariantCulture });
			}

			return o.ToString();
		}

		public static Vector2 ParseVector2(string s)
		{
			string[] split = s.Split(' ');
			if (split.Length == 2)
			{
				float x = float.Parse(split[0], CultureInfo.InvariantCulture);
				float y = float.Parse(split[1], CultureInfo.InvariantCulture);

				return new Vector2(x, y);
			}
			else
			{
				return Vector2.Zero;
			}
		}

		public static Vector3 ParseVector3(string s)
		{
			string[] split = s.Split(' ');
			if (split.Length == 3)
			{
				float x = float.Parse(split[0], CultureInfo.InvariantCulture);
				float y = float.Parse(split[1], CultureInfo.InvariantCulture);
				float z = float.Parse(split[2], CultureInfo.InvariantCulture);

				return new Vector3(x, y, z);
			}
			else
			{
				return Vector3.Zero;
			}
		}

		public static Vector4 ParseVector4(string s)
		{
			string[] split = s.Split(' ');
			if (split.Length == 4)
			{
				float x = float.Parse(split[0], CultureInfo.InvariantCulture);
				float y = float.Parse(split[1], CultureInfo.InvariantCulture);
				float z = float.Parse(split[2], CultureInfo.InvariantCulture);
				float w = float.Parse(split[3], CultureInfo.InvariantCulture);

				return new Vector4(x, y, z, w);
			}
			else
			{
				return Vector4.Zero;
			}
		}

		public static Quaternion ParseQuaternion(string s)
		{
			string[] split = s.Split(' ');
			if (split.Length == 4)
			{
				float x = float.Parse(split[0], CultureInfo.InvariantCulture);
				float y = float.Parse(split[1], CultureInfo.InvariantCulture);
				float z = float.Parse(split[2], CultureInfo.InvariantCulture);
				float w = float.Parse(split[3], CultureInfo.InvariantCulture);

				return new Quaternion(x, y, z, w);
			}
			else
			{
				return Quaternion.Identity;
			}
		}

		public static bool ParseBool(string s)
		{
			s = s.ToLowerInvariant();
			return s == "true" || s == "1" || s == "yes" || s == "enabled";
		}
	}
}
