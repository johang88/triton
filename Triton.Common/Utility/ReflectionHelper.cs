using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common.Utility
{
	public static class ReflectionHelper
	{
		/// <summary>
		/// Create a new instance of a named type, the type is required to have a constructor that takes no argumnets.
		/// The type is cast to a specific base class.
		/// </summary>
		/// <typeparam name="TType">Base class of the type that is created</typeparam>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static TType CreateInstance<TType>(string typeName)
		{
			return (TType)CreateInstance(typeName);
		}

		/// <summary>
		/// Create a new instance of a named type, the type is required to have a constructor that takes no argumnets
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static object CreateInstance(string typeName)
		{
			var appDomain = AppDomain.CurrentDomain;

			foreach (var assembly in appDomain.GetAssemblies())
			{
				var type = assembly.GetType(typeName, false);
				if (type != null)
				{
					return CreateInstance(type);
				}
			}

			throw new Exception("Could not create instance, type not found '" + typeName + "'");
		}

		public static TType CreateInstance<TType>(Type type)
		{
			return (TType)CreateInstance(type);
		}

		public static object CreateInstance(Type type)
		{
			var ctor = type.GetConstructor(new Type[0]);
			return ctor.Invoke(null);
		}

		public static bool HasField(Type type, string fieldName)
		{
			return type.GetField(fieldName) != null;
		}

		public static TType GetFieldValue<TType>(Type type, object obj, string fieldName)
		{
			var fieldInfo = type.GetField(fieldName);
			return (TType)fieldInfo.GetValue(obj);
		}

		public static object GetFieldValue(Type type, object obj, string fieldName)
		{
			var fieldInfo = type.GetField(fieldName);
			return fieldInfo.GetValue(obj);
		}

		/// <summary>
		/// Get all types that implements a speciific interface
		/// </summary>
		/// <param name="implement"></param>
		/// <returns></returns>
		public static IEnumerable<Type> GetTypesImplementing(Type implement)
		{
			var appDomain = AppDomain.CurrentDomain;

			foreach (var assembly in appDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (type.GetInterfaces().Contains(implement))
					{
						yield return type;
					}
				}
			}
		}

		/// <summary>
		/// Get all types implementing a specific interface
		/// </summary>
		/// <typeparam name="TBase"></typeparam>
		/// <returns></returns>
		public static IEnumerable<Type> GetTypesImplementing<TBase>()
		{
			return GetTypesImplementing(typeof(TBase));
		}

		/// <summary>
		/// Get all types derived from a specific type
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<Type> GetTypesDerivedFrom(Type deriveFrom)
		{
			var appDomain = AppDomain.CurrentDomain;

			foreach (var assembly in appDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					var baseType = type.BaseType;

					while (baseType != null)
					{
						if (baseType == deriveFrom)
						{
							yield return type;
						}
						baseType = baseType.BaseType;
					}
				}
			}
		}

		/// <summary>
		/// Get all types derived from a specific type
		/// </summary>
		/// <typeparam name="TBase"></typeparam>
		/// <returns></returns>
		public static IEnumerable<Type> GetTypesDerivedFrom<TBase>()
		{
			return GetTypesDerivedFrom(typeof(TBase));
		}
	}
}
