using static nopper.Nopper;
using System.Text;

namespace nopper
{
	internal class NopPack
	{
		public static void NOPPack(string outputFile, params string[] paths)
		{
			Nopper.Log($"== NOPPack: \"{paths[0]}\" ==\n");

			NOPType type = NOPType.NOP_DATA_DIRECTORY;

			using FileStream fs = new(outputFile, FileMode.Create);
			using BinaryWriter writer = new(fs);
			// Calculate key
			byte key = (byte)('d' ^ 0x15); // 'd' is the first letter of "data", 0x15 is the first byte of XOR'd "data" in valid .nop

			// Get XOR'd nameBytes
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			byte[] nameBytes = Encoding.GetEncoding("EUC-KR").GetBytes(paths[0]);
			for (int i = 0; i < nameBytes.Length; i++)
			{
				nameBytes[i] ^= key;
			}

			// Write metadata
			writer.Write((byte)nameBytes.Length); // name_size
			writer.Write((byte)type); // type should be 2 for directories

			// Get offset (offset now points to the start of the XOR'd name data)
			int offset = (int)fs.Position;

			// Write the remaining metadata
			writer.Write(offset); // offset
			writer.Write(0); // encode_size
			writer.Write((int)key); // decode_size as a 4-byte integer

			// Write XOR'd name data
			writer.Write(nameBytes);
			writer.Write((byte)0); // null byte

			// Get metadata offset (should be 0 for a directory with no files)
			int metadataOffset = 0;

			// Write offset to metadata and number of files
			writer.Write(metadataOffset);
			writer.Write(1); // num_files

			// Write end of file byte
			writer.Write((byte)0x12);
		}
	}
}
