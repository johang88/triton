using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Utility
{
	public class HashedString
	{
		/// <summary>
		/// Create a new hashed string, the name will always be converted to lower case using String.ToLowerInvariant.
		/// Instances should be reused as much as possible so that hash is never calculated more times than is necessary.
		/// 
		/// The string value of a hashed string can be looked up using HashStringTable.GetString
		/// </summary>
		/// <see cref="HashedStringTable"/>
		/// <param name="name">Name of the hashed string, used to generate an md5 hash</param>
		public HashedString(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("name is null or empty");
			}

			// Create an md5 hash sum of the name
			var provider = new MD5CryptoServiceProvider();
			var data = Encoding.ASCII.GetBytes(name);

			data = provider.ComputeHash(data);
			Hash = data[0];
			Hash = (Hash << 8) | data[1];
			Hash = (Hash << 8) | data[2];
			Hash = (Hash << 8) | data[3];

			// Add the hash and the name to the global hashed string table so that the name can be looked up later
			HashedStringTable.AddString(this, name);
		}

		/// <summary>
		/// Create an instance with a predefined hashed
		/// </summary>
		/// <param name="hash"></param>
		public HashedString(int hash)
		{
			Hash = hash;
		}

		public static bool operator ==(HashedString a, int b)
		{
			return (a.Hash == b);
		}

		public static bool operator !=(HashedString a, int b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj is HashedString)
			{
				HashedString other = obj as HashedString;
				return other.Hash == Hash;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Hash;
		}

		public override string ToString()
		{
			return string.Format("{0}", Hash);
		}

		public static implicit operator HashedString(string name)
		{
			return new HashedString(name);
		}

		public static implicit operator int(HashedString hashedString)
		{
			return hashedString.Hash;
		}

		int Hash;
	}
}
