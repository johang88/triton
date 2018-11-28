using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Triton.Utility
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
				Lookup.TryAdd(hashedString, value);
			}
			else if (value != Lookup[hashedString])
			{
				throw new Exception("hash collision");
			}
		}

		static ConcurrentDictionary<HashedString, string> Lookup = new ConcurrentDictionary<HashedString, string>();
	}
}
