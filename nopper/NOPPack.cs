namespace nopper
{
	internal class NopPack
	{
		public static void NOPPack(params string[] paths)
		{
			Console.WriteLine($"== NOPPack ==\n");

			foreach (string path in paths)
			{
				Console.WriteLine($"Path: \"{path}\"");
				// to do
			}
		}
	}
}
