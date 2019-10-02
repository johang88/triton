using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Utility;

namespace Triton.Graphics.Resources
{
	class BitmapFontSerializer : Triton.Resources.IResourceSerializer<BitmapFont>
	{
		private readonly Triton.IO.FileSystem _fileSystem;
		private readonly Triton.Resources.ResourceManager _resourceManager;

        public bool SupportsStreaming => false;

        public BitmapFontSerializer(Triton.Resources.ResourceManager resourceManager, Triton.IO.FileSystem fileSystem)
		{
            _fileSystem = fileSystem ?? throw new ArgumentNullException("fileSystem");
			_resourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
		}

		public string Extension { get { return ".fnt"; } }
		public string DefaultFilename { get { return ""; } }

        public object Create(Type type)
             => new BitmapFont(_resourceManager);

		public async Task Deserialize(object resource, byte[] data)
		{
			var bitmapFont = (BitmapFont)resource;

            using var stream = new System.IO.MemoryStream(data);
            using var reader = new System.IO.StreamReader(stream);

            while (stream.Position < stream.Length)
            {
                var line = reader.ReadLine();
                if (line.Length == 0)
                    continue;

                var commands = line.Split(' ');
                switch (commands[0])
                {
                    case "info": // Ignore
                        break;
                    case "common":
                        ParseCommonInfo(bitmapFont, ParseCommands(commands));
                        break;
                    case "page":
                        await ParsePageInfo(bitmapFont, ParseCommands(commands));
                        break;
                    case "chars": // Ignore
                        break;
                    case "char":
                        ParseCharInfo(bitmapFont, ParseCommands(commands));
                        break;
                }
            }
        }

		private Dictionary<string, string> ParseCommands(string[] commands)
		{
			var data = new Dictionary<string, string>();
			foreach (var command in commands)
			{
				if (command.Length == 0 || !command.Contains('='))
					continue;

				var splitCommand = command.Split('=');
				data.Add(splitCommand[0].Trim(), splitCommand[1].Trim());
			}

			return data;
		}

		private void ParseCommonInfo(BitmapFont bitmapFont, Dictionary<string, string> commands)
		{
			bitmapFont.LineHeight = StringConverter.Parse<float>(commands["lineHeight"]);
			bitmapFont.Scale = new Vector2(StringConverter.Parse<float>(commands["scaleW"]), StringConverter.Parse<float>(commands["scaleH"]));
		}

		private async Task ParsePageInfo(BitmapFont bitmapFont, Dictionary<string, string> commands)
		{
			var filename = commands["file"].Replace("\"", "");
			bitmapFont.Textures.Add(await _resourceManager.LoadAsync<Texture>(filename));
		}

		private void ParseCharInfo(BitmapFont bitmapFont, Dictionary<string, string> commands)
		{
			var id = StringConverter.Parse<int>(commands["id"]);
			var x = StringConverter.Parse<int>(commands["x"]);
			var y = StringConverter.Parse<int>(commands["y"]);
			var width = StringConverter.Parse<int>(commands["width"]);
			var height = StringConverter.Parse<int>(commands["height"]);
			var xoffset = StringConverter.Parse<int>(commands["xoffset"]);
			var yoffset = StringConverter.Parse<int>(commands["yoffset"]);
			var xadvance = StringConverter.Parse<int>(commands["xadvance"]);
			var page = StringConverter.Parse<int>(commands["page"]);

			var uvSize = new Vector2(width / bitmapFont.Scale.X, height / bitmapFont.Scale.Y);

			bitmapFont.Glyphs.Add((char)id, new BitmapFont.Glyph()
			{
				UvPosition = new Vector2(x / bitmapFont.Scale.X, y / bitmapFont.Scale.Y + uvSize.Y),
				UvSize = new Vector2(uvSize.X, -uvSize.Y),
				Size = new Vector2(width, height),
				Offset = new Vector2(xoffset, yoffset),
				XAdvance = xadvance,
				Page = page
			});
		}

        public byte[] Serialize(object resource)
            => throw new NotImplementedException();
    }
}
