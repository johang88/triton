using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools
{
	public class CustomTypeDescriptor : System.ComponentModel.CustomTypeDescriptor
	{
		private readonly object Instance;

		public CustomTypeDescriptor(object instance)
			: base()
		{
			Instance = instance;
		}

		public override PropertyDescriptorCollection GetProperties()
		{
			var properties = new List<PropertyDescriptor>();

			foreach (var property in Helpers.GetSerializableProperties(Instance.GetType(), false))
			{
				var tempAttributes = property.GetCustomAttributes(true);
				var attributes = new Attribute[tempAttributes.Length];
				tempAttributes.CopyTo(attributes, 0);

				properties.Add(new CustomPropertyDescriptor(Instance, property, attributes));
			}

			return new PropertyDescriptorCollection(properties.ToArray());
		}

		public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			return GetProperties();
		}
	}
}
