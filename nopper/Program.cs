using System;
using System.IO;
using System.Text;

public class NOPper
{
	const int BUFFER_SIZE = 1024 * 1024 * 256;
	const int MAX_LZ77_LENGTH = 18;
	const int WINDOW_SIZE = 0x1000;

	static byte[]? buff1 = new byte[BUFFER_SIZE];
	static byte[]? buff2 = new byte[BUFFER_SIZE];

	static ushort[] lz77_customkey = { 0xFF21, 0x834F, 0x675F, 0x0034, 0xF237, 0x815F, 0x4765, 0x0233 };

	enum NOPType
	{
		NOP_DATA_RAW = 0x00,
		NOP_DATA_LZ77 = 0x01,
		NOP_DATA_DIRECTORY = 0x02,
		NOP_DATA_SONNORI_LZ77 = 0x03
	};

	public static void Main()
	{
		buff1 = new byte[BUFFER_SIZE];
		buff2 = new byte[BUFFER_SIZE];

		string InputFile = $"{Directory.GetCurrentDirectory()}\\whiteday120.nop";

		Console.WriteLine($"Input: {Path.GetFileName(InputFile)}\n");
		Console.ReadLine();

		NOPUnpack(InputFile);

		buff1 = null;
		buff2 = null;
	}

	static void NOPUnpack(string NOP)
	{
		string NOPFileName = Path.GetFileName(NOP);

		using (FileStream fs = new(NOP, FileMode.Open, FileAccess.Read))
		{
			using (BinaryReader reader = new(fs))
			{
				// Check if file can be opened
				if (!File.Exists(NOP))
				{
					Console.WriteLine($"Failed to open file: {NOPFileName}");
					return;
				}

				// Check if file is corrupted
				fs.Seek(-1, SeekOrigin.End);
				if (reader.ReadByte() != 0x12)
				{
					Console.WriteLine($"{NOPFileName} is corrupted.");
					return;
				}

				fs.Seek(-9, SeekOrigin.End);
				int off = reader.ReadInt32();
				int num = reader.ReadInt32();
				Console.WriteLine($"{NOPFileName} contains a total of {num} items.\n");
				Thread.Sleep(500);

				// Process each data block
				for (int i = 0; i < num; ++i)
				{
					byte key = 0;
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
						key = (byte)decode_size;
					}
					else
					{
						decode_size ^= key;
					}

					for (int j = 0; j < name_size; ++j)
						name[j] ^= key;

					Console.WriteLine($"{(i * 100) / num}%: {Encoding.Default.GetString(name)} ({Enum.GetName(typeof(NOPType), type)})");
					Console.ReadLine();

					byte[] buff1 = new byte[encode_size];
					byte[] buff2 = new byte[decode_size]; // Make sure it is large enough

					// Trim null bytes from the end of 'name'
					name = name.TakeWhile(b => b != 0).ToArray();
					string fileName = Encoding.Default.GetString(name);

					switch (type)
					{
						case (byte)NOPType.NOP_DATA_RAW:
							{
								fs.Seek(offset, SeekOrigin.Begin);
								reader.Read(buff1, 0, encode_size);
								using (FileStream outFs = new(Encoding.Default.GetString(name), FileMode.Create))
								{
									outFs.Write(buff1, 0, decode_size);
								}
								break;
							}
						case (byte)NOPType.NOP_DATA_DIRECTORY:
							{
								Console.WriteLine($"Creating directory: {Encoding.Default.GetString(name)}");
								Console.ReadLine();
								Directory.CreateDirectory($"{Encoding.Default.GetString(name)}");
								break;
							}
						case (byte)NOPType.NOP_DATA_LZ77:
							{
								// Implement the LZ77 decoding logic here
								// ...
								break;
							}
						case (byte)NOPType.NOP_DATA_SONNORI_LZ77:
							{
								// Implement the SONNORI_LZ77 decoding logic here
								// ...
								break;
							}
						default:
							{
								Console.WriteLine($"Failed: {Encoding.Default.GetString(name)}");
								break;
							}
					}
				}
			}
		}
	}
}
