using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools
{
	public class CustomPropertyDescriptor : PropertyDescriptor
	{
		private readonly object Instance;
		private readonly MemberInfo MemberInfo;

		public CustomPropertyDescriptor(object instance, MemberInfo memberInfo, Attribute[] attributes)
			: base(memberInfo.Name, attributes)
		{
			Instance = instance;
			MemberInfo = memberInfo;
		}

		public override bool CanResetValue(object component)
		{
			return false;
		}

		public override Type ComponentType
		{
			get { throw new NotImplementedException(); }
		}

		public override object GetValue(object component)
		{
			if (MemberInfo is PropertyInfo)
			{
				var info = MemberInfo as PropertyInfo;
				return info.GetValue(Instance, null);
			}
			else
			{
				var info = MemberInfo as FieldInfo;
				return info.GetValue(Instance);
			}
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override Type PropertyType
		{
			get
			{
				if (MemberInfo is PropertyInfo)
				{
					var info = MemberInfo as PropertyInfo;
					return info.PropertyType;
				}
				else
				{
					var info = MemberInfo as FieldInfo;
					return info.FieldType;
				}
			}
		}

		public override void ResetValue(object component)
		{
			throw new NotImplementedException();
		}

		public override void SetValue(object component, object value)
		{
			if (MemberInfo is PropertyInfo)
			{
				var info = MemberInfo as PropertyInfo;
				info.SetValue(Instance, value, null);
			}
			else
			{
				var info = MemberInfo as FieldInfo;
				info.SetValue(Instance, value);
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}

		public override TypeConverter Converter
		{
			get
			{
				if (PropertyType == typeof(Vector3))
					return new TypeConverters.Vector3TypeConverter();
				else if (PropertyType == typeof(Vector4))
					return new TypeConverters.Vector4TypeConverter();
				else if (PropertyType == typeof(Quaternion))
					return new TypeConverters.QuaternionTypeConverter();
				else
					return base.Converter;
			}
		}
	}
}
