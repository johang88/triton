using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Triton.Content.Database
{
	[Table("entries")]
	public class ContentEntry
	{
		[PrimaryKey, Column("id")]
		public string Id { get; set; }
		[Indexed, Column("source")]
		public string Source { get; set; }
		[MaxLength(16), Column("type")]
		public string Type { get; set; }
		[Column("last_compilation")]
		public DateTime LastCompilation { get; set; }
	}
}
