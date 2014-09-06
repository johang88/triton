using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeGraphControl
{
	public static class ReflectionHelper
	{
		public static object CreateInstance(string typeName, object[] arguments)
		{
			var appDomain = AppDomain.CurrentDomain;

			foreach (var assembly in appDomain.GetAssemblies())
			{
				var type = assembly.GetType(typeName, false);
				if (type != null)
				{
					return CreateInstance(type, arguments);
				}
			}

			throw new Exception("Could not create instance, type not found '" + typeName + "'");
		}

		public static object CreateInstance(Type type, object[] arguments)
		{
			var ctor = type.GetConstructor(arguments.Select(a => a.GetType()).ToArray());
			return ctor.Invoke(arguments);
		}
	}
}
