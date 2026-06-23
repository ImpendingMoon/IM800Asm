namespace IM800Asm
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: IM800Asm <source file> <output file>");
				return;
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

			string source = File.ReadAllText(sourceFilePath);

			Result result = new();

			Lexer lexer = new(source);

			Result<List<Token>> tokenizeResult = lexer.Tokenize();
			result.Combine(tokenizeResult);

			GrammarParser grammarParser = new(tokenizeResult.ResultObject);

			Result<List<Statement>> grammarParseResult = grammarParser.Parse();
			result.Combine(grammarParseResult);

			Console.WriteLine("=== WARNINGS ===");
			foreach (Result.Error warning in result.Warnings)
			{
				Console.WriteLine(warning);
			}

			Console.WriteLine();

			Console.WriteLine("=== ERRORS ===");
			foreach (Result.Error error in result.Errors)
			{
				Console.WriteLine(error);
			}

			Console.WriteLine();

			Console.WriteLine("=== TOKENS ===");
			foreach (Token token in tokenizeResult.ResultObject)
			{
				Console.WriteLine(token);
			}
			Console.WriteLine();

			Console.WriteLine("=== STATEMENTS ===");
			foreach (Statement statement in grammarParseResult.ResultObject)
			{
				Console.WriteLine(statement);
			}
		}
	}
}
