using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools
{
	public static class Helpers
	{
		public static IEnumerable<MemberInfo> GetSerializableProperties(Type type, bool privateFields = true)
		{
			var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
			if (privateFields)
				bindingFlags |= BindingFlags.NonPublic;

			foreach (var propertyInfo in type.GetProperties(bindingFlags))
			{
				if (!propertyInfo.IsDefined(typeof(IgnoreDataMemberAttribute), true))
					yield return propertyInfo;
			}

			foreach (var fieldInfo in type.GetFields(bindingFlags))
			{
				if (!fieldInfo.IsDefined(typeof(IgnoreDataMemberAttribute), true))
					yield return fieldInfo;
			}
		}
	}
}
