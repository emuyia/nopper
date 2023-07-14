using System.Text;

namespace nopper
{
	internal class Nopper
	{
		public enum NOPType
		{
			NOP_DATA_RAW = 0x00,
			NOP_DATA_LZ77 = 0x01,
			NOP_DATA_DIRECTORY = 0x02,
			NOP_DATA_SONNORI_LZ77 = 0x03
		};

		public static readonly ushort[] lz77_customkey = { 0xFF21, 0x834F, 0x675F, 0x0034, 0xF237, 0x815F, 0x4765, 0x0233 };

		public static void Main(string[] args)
		{
			Console.OutputEncoding = Encoding.UTF8;
			Console.WriteLine("== nopper ==");

			string[] NOPFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.nop");
			Array.Sort(NOPFiles);

			if (args.Length == 0 && NOPFiles.Length == 0)
			{
				Console.WriteLine("No NOP files were found.\n" +
					"\nEither:" +
					"\n(1) Place your NOP file(s) in the current directory and try again" +
					"\n(2) Open your NOP file(s) using this program" +
					"\n(3) Use the program from the command line:" +
					"\n	- Unpack: \"nopper unpack <item>\"" +
					"\n	- Pack:   \"nopper pack <item1> <item2> ...\"");

				Console.ReadLine();
				return;
			}

			var filesToProcess = args.Length > 0 ? args : NOPFiles;

			if (filesToProcess.All(path => Path.GetExtension(path).Equals(".nop")))
			{
				foreach (string path in filesToProcess)
				{
					NopUnpack.NOPUnpack(path);
				}
			}
			else if (args.Length > 0)
			{
				string command = args[0];
				string[] commandArgs = args.Skip(1).ToArray();

				switch (command)
				{
					case "unpack":
						if (commandArgs.Length > 0)
							foreach (var arg in commandArgs) NopUnpack.NOPUnpack(arg);
						else
							Console.WriteLine("Usage: \"nopper unpack <item>\"");
						break;
					case "pack":
						if (commandArgs.Length > 0)
							NopPack.NOPPack(commandArgs);
						else
							Console.WriteLine("Usage: \"nopper pack <item1> <item2> ...\"");
						break;
					default:
						NopPack.NOPPack(args);
						break;
				}
			}
		}
	}
}
