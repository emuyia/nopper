using System.Text;

public class nopper
{
	const int BUFFER_SIZE = 1024 * 1024 * 256;

	static byte[]? buff1 = new byte[BUFFER_SIZE];
	static byte[]? buff2 = new byte[BUFFER_SIZE];

	static readonly ushort[] lz77_customkey = { 0xFF21, 0x834F, 0x675F, 0x0034, 0xF237, 0x815F, 0x4765, 0x0233 };

	enum NOPType
	{
		NOP_DATA_RAW = 0x00,
		NOP_DATA_LZ77 = 0x01,
		NOP_DATA_DIRECTORY = 0x02,
		NOP_DATA_SONNORI_LZ77 = 0x03
	};

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
				NOPUnpack(path);
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
						foreach (var arg in commandArgs) NOPUnpack(arg);
					else
						Console.WriteLine("Usage: \"nopper unpack <item>\"");
					break;
				case "pack":
					if (commandArgs.Length > 0)
						NOPPack(commandArgs);
					else
						Console.WriteLine("Usage: \"nopper pack <item1> <item2> ...\"");
					break;
				default:
					NOPPack(args);
					break;
			}
		}

		buff1 = null;
		buff2 = null;
	}

	static void NOPUnpack(string NOP)
	{
		string NOPFileName = Path.GetFileName(NOP);

		Console.WriteLine($"== NOPUnpack: \"{NOPFileName}\" ==\n");

		using (FileStream fs = new(NOP, FileMode.Open, FileAccess.Read))
		{
			using (BinaryReader reader = new(fs))
			{
				// Check if file can be opened
				if (!File.Exists(NOP))
				{
					Console.WriteLine($"Failed to open \"{NOPFileName}\"");
					return;
				}

				// Check if file is corrupted
				fs.Seek(-1, SeekOrigin.End);
				if (reader.ReadByte() != 0x12)
				{
					Console.WriteLine($"\"{NOPFileName}\" is corrupted.");
					return;
				}

				fs.Seek(-9, SeekOrigin.End);
				int off = reader.ReadInt32();
				int num = reader.ReadInt32();
				Console.WriteLine($"\"{NOPFileName}\" contains {num} items");

				byte key = 0;

				// Process each data block
				for (int i = 0; i < num; ++i)
				{
					Console.WriteLine();

					byte[] name = new byte[256];
					fs.Seek(off, SeekOrigin.Begin);
					byte name_size = reader.ReadByte();
					byte type = reader.ReadByte();
					int offset = reader.ReadInt32();
					int encode_size = reader.ReadInt32();
					int decode_size = reader.ReadInt32();
					reader.Read(name, 0, name_size + 1);
					off += name_size + 15;

					if (type == (byte)NOPType.NOP_DATA_DIRECTORY)
					{
						key = (byte)(decode_size & 0xFF); // Ensure key is within byte range
					}
					else
					{
						decode_size ^= key;
					}

					for (int j = 0; j < name_size; ++j)
						name[j] = (byte)(name[j] ^ key);

					// Trim null bytes from the end of 'name'
					name = name.TakeWhile(b => b != 0).ToArray();

					// Use EUC-KR encoding
					Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
					string fileName = Encoding.GetEncoding("EUC-KR").GetString(name, 0, name_size);

					// Debug info
					Console.WriteLine($"== Progress: {(i * 100) / num}% ==");
					Console.WriteLine($"offset=\"{offset}\", encode_size=\"{encode_size}\", decode_size=\"{decode_size}\", name=\"{name}\", name_size=\"{name_size}\", key=\"{key}\", fileName=\"{fileName}\", type=\"{Enum.GetName(typeof(NOPType), type)}\"");


					switch (type)
					{
						case (byte)NOPType.NOP_DATA_RAW:
							{
								fs.Seek(offset, SeekOrigin.Begin);
								reader.Read(buff1, 0, encode_size);

								Console.WriteLine($"Writing file: \"{fileName}\"");

								using (FileStream outFs = new(fileName, FileMode.Create, FileAccess.Write))
								{
									outFs.Write(buff1, 0, decode_size);
								}
								break;
							}
						case (byte)NOPType.NOP_DATA_DIRECTORY:
							{
								Console.WriteLine($"Creating directory: \"{fileName}\"");
								Directory.CreateDirectory($"{fileName}");
								break;
							}
						case (byte)NOPType.NOP_DATA_LZ77:
							{
								fs.Seek(offset, SeekOrigin.Begin);
								if (encode_size > buff1.Length) encode_size = buff1.Length;
								fs.Read(buff1, 0, encode_size);
								int bmask = 0, bcnt = 0, size = 0, offs, len;
								ushort Lz77Info;
								for (int j = 0; j < encode_size && size < buff2.Length; bcnt = (bcnt + 1) & 0x07)
								{
									if (bcnt == 0)
									{
										bmask = buff1[j++];
									}
									else
									{
										bmask >>= 1;
									}
									if ((bmask & 0x01) != 0)
									{
										Lz77Info = BitConverter.ToUInt16(buff1, j);
										j += 2;
										offs = Lz77Info & 0x0FFF;
										len = (Lz77Info >> 12) + 2;
										if (size >= offs && size + len <= buff2.Length)
										{
											Buffer.BlockCopy(buff2, size - offs, buff2, size, len);
											size += len;
										}
									}
									else if (j < buff1.Length)
									{
										buff2[size++] = buff1[j++];
									}
								}
								if (size != decode_size)
								{
									Console.WriteLine($"Failed to write file: fileName=\"{fileName}\" (size={size} != decode_size=\"{decode_size}\")");
									break;
								}

								Console.WriteLine($"Writing file: \"{fileName}\"");

								using (var fs2 = new FileStream(fileName, FileMode.Create, FileAccess.Write))
								{
									fs2.Write(buff2, 0, decode_size);
								}
								break;
							}
						case (byte)NOPType.NOP_DATA_SONNORI_LZ77:
							{
								unsafe
								{
									fs.Seek(offset, SeekOrigin.Begin);
									if (encode_size > buff1.Length) encode_size = buff1.Length;
									fs.Read(buff1, 0, encode_size);
									int bmask = 0, bsrcmask = 0, bcnt = 0, size = 0, offs, len;
									ushort Lz77Info;
									for (int j = 0; j < encode_size && size < buff2.Length; bcnt = (bcnt + 1) & 0x07)
									{
										if (bcnt == 0)
										{
											bmask = bsrcmask = buff1[j++];
											bmask ^= 0xC8;
										}
										else
										{
											bmask >>= 1;
										}
										if ((bmask & 0x01) != 0)
										{
											Lz77Info = BitConverter.ToUInt16(buff1, j);
											j += 2;
											Lz77Info ^= lz77_customkey[(bsrcmask >> 3) & 0x07];
											offs = Lz77Info & 0x0FFF;
											len = (Lz77Info >> 12) + 2;
											if (size >= offs && size + len <= buff2.Length)
											{
												Buffer.BlockCopy(buff2, size - offs, buff2, size, len);
												size += len;
											}
										}
										else if (j < buff1.Length)
										{
											buff2[size++] = buff1[j++];
										}
									}
									if (size != decode_size)
									{
										Console.WriteLine($"Failed to write file: fileName=\"{fileName}\" (size={size} != decode_size=\"{decode_size}\")");
										break;
									}

									Console.WriteLine($"Writing file: \"{fileName}\"");

									using (var fs2 = new FileStream(fileName, FileMode.Create, FileAccess.Write))
									{
										fs2.Write(buff2, 0, decode_size);
									}
									break;
								}
							}
						default:
							{
								Console.WriteLine($"Failed to write: \"{fileName}\" (unknown data type)");
								break;
							}
					}
				}
			}
		}
	}

	static void NOPPack(params string[] paths)
	{
		foreach (string path in paths)
		{
			Console.WriteLine($"Path: \"{path}\"");
			// to do
		}
	}
}
