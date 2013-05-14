using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
	/// <summary>
	/// Provides lookups from hashed strings to the actual string
	/// This class will also detect hash collisions
	/// </summary>
	public static class HashedStringTable
	{
		public static string GetString(HashedString hashedString)
		{
			if (Lookup.ContainsKey(hashedString))
				return Lookup[hashedString];
			else
				return "";
		}

		public static void AddString(HashedString hashedString, string value)
		{
			if (!Lookup.ContainsKey(hashedString))
			{
				Lookup.Add(hashedString, value);
			}
			else if (value != Lookup[hashedString])
			{
				throw new Exception("hash collision");
			}
		}

		static Dictionary<HashedString, string> Lookup = new Dictionary<HashedString, string>();
	}
}
