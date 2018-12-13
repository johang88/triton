using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Triton.Logging
{
	public class File : ILogOutputHandler
	{
		private readonly string Filename;

		public File(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
				throw new ArgumentNullException("filename");

			Filename = filename;

			string directory = Path.GetDirectoryName(filename);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false, Encoding.ASCII);
			file.Write(string.Format("Log file created: {0}{1}", DateTime.Now, System.Environment.NewLine));
			file.Flush();
			file.Close();
		}

		public void WriteLine(string message, LogLevel level)
		{
			StringBuilder sb = new StringBuilder();
			switch (level)
			{
				case LogLevel.Info:
					sb.Append("<-> ").Append(message);
					break;
				case LogLevel.Warning:
					sb.Append("<!> ").Append(message);
					break;
				case LogLevel.Error:
					sb.Append("<X> ").Append(message);
					break;
				case LogLevel.Debug:
#if DEBUG
					sb.Append("<D> ").Append(message);
#endif
					break;
			}

			sb.Append(System.Environment.NewLine);

			System.IO.StreamWriter file = new System.IO.StreamWriter(Filename, true, Encoding.UTF8);

			file.Write(sb.ToString());
			file.Flush();

			file.Close();
		}
	}
}
