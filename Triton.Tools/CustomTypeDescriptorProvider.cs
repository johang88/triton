using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools
{
	public class CustomTypeDescriptorProvider : TypeDescriptionProvider
	{
		public static void Register(Type type)
		{
			TypeDescriptor.AddProvider(new CustomTypeDescriptorProvider(), type);
		}

		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
		{
			return new CustomTypeDescriptor(instance);
		}
	}
}
