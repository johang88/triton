using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Content.Database
{
	public class DB : IDisposable
	{
		private readonly SQLiteConnection Connection;

		public DB(string path)
		{
			Connection = new SQLiteConnection(path);
			Connection.CreateTable<ContentEntry>();
		}

		public void Dispose()
		{
			Connection.Dispose();
		}

		public List<ContentEntry> GetAllEntries()
		{
			return Connection.Table<ContentEntry>().ToList();
		}

		public void AddEntry(ContentEntry entry)
		{
			Connection.Insert(entry);
		}

		/// <summary>
		/// Checks if there exists a content entry with the specified source path
		/// This is used to automatically import new source content
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool SourceExists(string source)
		{
			return Connection.Table<ContentEntry>().Where(c => c.Source == source).Count() > 0;
		}

		public void SaveEntry(ContentEntry entry)
		{
			Connection.Update(entry);
		}
	}
}
