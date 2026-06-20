namespace IM800Asm
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: IM800Asm <source file> <output file>");
            }

            string sourceFilePath = args[0];
            sourceFilePath = sourceFilePath.Trim('"');

            if (sourceFilePath.StartsWith('~'))
            {
                string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                sourceFilePath = sourceFilePath.Replace("~", userFolder);
            }

            if (!File.Exists(sourceFilePath))
            {
                Console.WriteLine($"Cannot find the file \"{sourceFilePath}\"");
                return;
            }

            string[] sourceLines = File.ReadAllLines(sourceFilePath);


        }
    }
}
