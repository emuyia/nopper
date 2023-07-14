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


			Console.WriteLine("Writing buffer...");
			fs.Write(buff, 0, fileSize);


			Console.WriteLine("Writing name_size byte...");
			byte nameSize = (byte)fileName.Length;
			writer.Write(nameSize); // write length of file name

			Console.WriteLine("Writing data type byte...");
			writer.Write((byte)Nopper.NOPType.NOP_DATA_RAW); // write stored data type

			Console.WriteLine($"Writing fileSize ({fileSize})...");
			writer.Write(fileSize); // write original file size


			long metadataOffsetPos = fs.Position;

			// Write placeholders for the metadata offset and number of files
			writer.Write(0);
			writer.Write(1);


			// Write the end of metadata byte
			Console.WriteLine("Writing end byte...");
			writer.Write((byte)0x12); // start of metadata section


			long endPos = fs.Position;

			// Go back and update the metadata offset and number of files
			Console.WriteLine("Writing offset byte...");
			fs.Position = metadataOffsetPos;
			writer.Write((int)endPos); // size of file


			Console.WriteLine("Writing num of items byte...");
			writer.Write(1); // num of items
			

			writer.Flush();

			//Console.ReadLine();
		}
	}
}
