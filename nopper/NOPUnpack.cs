using System.Text;

namespace nopper
{
	internal class NopUnpack
	{
		public static void NOPUnpack(string NOP)
		{
			string NOPFileName = Path.GetFileName(NOP);

			Console.WriteLine($"== NOPUnpack: \"{NOPFileName}\" ==\n");

			int BUFFER_SIZE = 1024 * 1024 * 256;
			byte[]? buff1 = new byte[BUFFER_SIZE];
			byte[]? buff2 = new byte[BUFFER_SIZE];

			using (FileStream fs = new(NOP, FileMode.Open, FileAccess.Read))
			{
				using BinaryReader reader = new(fs);
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

					if (type == (byte)Nopper.NOPType.NOP_DATA_DIRECTORY)
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
					Console.WriteLine($"== Unpacking item {i + 1} of {num}... ==");
					Console.WriteLine($"fileName=\"{fileName}\", type=\"{Enum.GetName(typeof(Nopper.NOPType), type)}\"" +
						$"\noffset={offset}, encode_size={encode_size}, decode_size={decode_size}, name_size=\"{name_size}\", key={key}");


					switch (type)
					{
						default:
							{
								Console.WriteLine($"Failed to write: \"{fileName}\" (unknown data type)");
								break;
							}
						case (byte)Nopper.NOPType.NOP_DATA_DIRECTORY:
							{
								Console.WriteLine($"Writing data: \"{fileName}\"");
								Directory.CreateDirectory($"{fileName}");
								break;
							}
						case (byte)Nopper.NOPType.NOP_DATA_RAW:
							{
								fs.Seek(offset, SeekOrigin.Begin);
								reader.Read(buff1, 0, encode_size);
								WriteFile(buff1, fileName, decode_size);
								break;
							}
						case (byte)Nopper.NOPType.NOP_DATA_LZ77:
							{
								if (!DecodeLZ77(fs, buff1, buff2, offset, encode_size, decode_size, false)) break;
								WriteFile(buff2, fileName, decode_size);
								break;
							}
						case (byte)Nopper.NOPType.NOP_DATA_SONNORI_LZ77:
							{
								if (!DecodeLZ77(fs, buff1, buff2, offset, encode_size, decode_size, true)) break;
								WriteFile(buff2, fileName, decode_size);
								break;
							}
					}
				}
			}
			buff1 = null;
			buff2 = null;
		}

		private static bool DecodeLZ77(FileStream fs, byte[] buff1, byte[] buff2, int offset, int encode_size, int decode_size, bool sonnori = false)
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
					if (sonnori) bmask ^= 0xC8;
				}
				else
				{
					bmask >>= 1;
				}
				if ((bmask & 0x01) != 0)
				{
					Lz77Info = BitConverter.ToUInt16(buff1, j);
					j += 2;
					if (sonnori) Lz77Info ^= Nopper.lz77_customkey[(bsrcmask >> 3) & 0x07];
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
				Console.WriteLine($"Failed to decode LZ77 compression - \"size\" ({size}) is not equal to \"decode_size\" ({decode_size})");
				return false;
			}
			else
			{
				Console.WriteLine($"Successfully decoded LZ77 compression - \"size\" ({size}) is equal to \"decode_size\" ({decode_size})");
				return true;
			}
		}

		private static bool WriteFile(byte[] buff, string path, int decode_size)
		{
			Console.Write($"Writing data: \"{path}\"...");
			try
			{
				using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
				fs.Write(buff, 0, decode_size);
			}
			catch (Exception e)
			{
				Console.WriteLine($" failed!\nError: \"{e.Message}\"");
				return false;
			}
			Console.WriteLine($" success!");
			return true;
		}
	}

}
