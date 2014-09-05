using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools.TypeConverters
{
	public class Vector3TypeConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (destinationType == typeof(Vector3))
				return true;

			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
										 Type destinationType)
		{
			if (destinationType == typeof(string) && value is Vector3)
			{
				var v = (Vector3)value;

				return Triton.Common.StringConverter.ToString(v);
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
				return Triton.Common.StringConverter.ParseVector3(value as string);
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}
