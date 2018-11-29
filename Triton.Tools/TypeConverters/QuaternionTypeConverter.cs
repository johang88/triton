using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools.TypeConverters
{
	public class QuaternionTypeConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(Quaternion))
				return true;

			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
										 Type destinationType)
		{
			if (destinationType == typeof(string) && value is Quaternion)
			{
				var v = (Quaternion)value;

				return Triton.Utility.StringConverter.ToString(v);
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(string))
				return true;
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string)
			{
				return Triton.Utility.StringConverter.ParseQuaternion(value as string);
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
