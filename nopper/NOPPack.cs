namespace nopper
{
	internal class NopPack
	{
		public static void NOPPack(string outputFile, params string[] paths)
		{

			/*foreach (string path in paths)
			{
				Console.WriteLine("NOPPack is not available yet.");
			}*/


			string outputPath = $"{Directory.GetCurrentDirectory()}\\{outputFile}";

			// Testing on only one file for now
			string filePath = $"{Directory.GetCurrentDirectory()}\\{paths[0]}";
			string fileName = Path.GetFileName(filePath);

			Console.WriteLine($"== NOPPack: \"{fileName}\" ==\n");

			int BUFFER_SIZE = 1024 * 1024 * 256;
			byte[] buff = new byte[BUFFER_SIZE];

			int fileSize;

			// First, read the original file
			using (FileStream fsOrigin = new(filePath, FileMode.Open, FileAccess.Read))
			{
				fileSize = (int)fsOrigin.Length;
				fsOrigin.Read(buff, 0, fileSize);
			}

			// Now, write to the .nop file
			using FileStream fs = new(outputPath, FileMode.OpenOrCreate, FileAccess.Write);
			using BinaryWriter writer = new(fs);

			Console.WriteLine("Writing end byte...");
			fs.Seek(0, SeekOrigin.End);
			writer.Write((byte)0x12); // start of metadata section

			// Ensure the file is long enough to seek backwards 9 bytes, otherwise pad with zeros
			while (fs.Length < 9)
			{
				writer.Write((byte)0x00);
			}

			Console.WriteLine("Writing offset byte...");
			fs.Seek(-9, SeekOrigin.End);
			int offset = (int)fs.Length;
			writer.Write(offset); // size of file
			writer.Write(1); // num of items

			Console.WriteLine("Writing name_size byte...");
			byte nameSize = (byte)fileName.Length;
			writer.Write(nameSize); // write length of file name

			Console.WriteLine("Writing data type byte...");
			writer.Write((byte)Nopper.NOPType.NOP_DATA_RAW); // write stored data type

			Console.WriteLine($"Writing fileSize ({fileSize})...");
			writer.Write(fileSize); // write original file size

			// Seek to the offset recorded in the metadata section and write the original file's data to that position
			Console.WriteLine("Writing buffer...");
			fs.Seek(offset, SeekOrigin.Begin);
			fs.Write(buff, 0, fileSize);

			writer.Flush();

			Console.ReadLine();
		}
	}
}
