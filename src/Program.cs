using System.Diagnostics;

namespace IM800Asm;

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

		string outputFilePath = args[1];
		outputFilePath = outputFilePath.Trim('"');

		if (outputFilePath.StartsWith('~'))
		{
			string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			outputFilePath = outputFilePath.Replace("~", userFolder);
		}

		Stopwatch stopwatch = new();
		stopwatch.Start();

		string source = File.ReadAllText(sourceFilePath);

		Result result = new();

		// Temporary driver while I develop the thing

		Lexer lexer = new(source);

		Result<List<Token>> tokenizeResult = lexer.Tokenize();
		result.Combine(tokenizeResult);

		Parser parser = new(tokenizeResult.ResultObject);

		Result<List<Statement>> parseResult = parser.Parse();
		result.Combine(parseResult);

		Assembler assembler = new(parseResult.ResultObject);
		Result<List<byte>> assemblerResult = assembler.Assemble();
		result.Combine(assemblerResult);

		if (result.IsSuccess)
		{
			File.WriteAllBytes(outputFilePath, assemblerResult.ResultObject.ToArray());
		}
		stopwatch.Stop();

		Console.WriteLine($"Assembled {assemblerResult.ResultObject.Count} bytes in {stopwatch.ElapsedMilliseconds / 1000.0} seconds");
		Console.WriteLine();

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

		Console.WriteLine("=== SYMBOLS ===");
		foreach (var kvp in assembler.SymbolTable.OrderBy(x => x.Value))
		{
			if (kvp.Value > 0)
			{
				Console.WriteLine($"{kvp.Key}:\t{kvp.Value:X8}");
			}
			else
			{
				Console.WriteLine($"{kvp.Key}:\t{kvp.Value}");
			}
		}

		Console.WriteLine();

		// yum yum 18000 line output for my "test everything" source file
		// Console.WriteLine("=== TOKENS ===");
		// foreach (Token token in tokenizeResult.ResultObject)
		// {
		// 	Console.WriteLine(token);
		// }
		// Console.WriteLine();
		// Console.WriteLine("=== STATEMENTS ===");
		// foreach (Statement statement in parseResult.ResultObject)
		// {
		// 	Console.WriteLine(statement);
		// }
		// Console.WriteLine();
	}
}
