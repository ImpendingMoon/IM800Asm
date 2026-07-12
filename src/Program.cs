using System.Diagnostics;

namespace IM800Asm;

internal static class Program
{
	private static int Main(string[] args)
	{
		if (args.Length == 0)
		{
			PrintUsage();
			return 1;
		}

		string? inputFile = null;
		string? outputFile = null;
		string? symbolFile = null;
		string? listingFile = null;

		for (int i = 0; i < args.Length; i++)
		{
			switch (args[i])
			{
				case "--help":
				case "-h":
					PrintUsage();
					return 0;

				case "--test":
					if (++i >= args.Length)
					{
						Console.Error.WriteLine("error: --test requires a file.");
						return 1;
					}

					RunTests(ExpandPath(args[i]));
					return 0;

				case "-o":
					if (++i >= args.Length)
					{
						Console.Error.WriteLine("error: -o requires a filename.");
						return 1;
					}

					outputFile = ExpandPath(args[i]);
					break;

				case "-s":
					if (++i >= args.Length)
					{
						Console.Error.WriteLine("error: -s requires a filename.");
						return 1;
					}

					symbolFile = ExpandPath(args[i]);
					break;

				case "-l":
					if (++i >= args.Length)
					{
						Console.Error.WriteLine("error: -l requires a filename.");
						return 1;
					}

					listingFile = ExpandPath(args[i]);
					break;

				default:
					if (args[i].StartsWith('-'))
					{
						Console.Error.WriteLine($"error: unknown option '{args[i]}'.");
						return 1;
					}

					if (inputFile != null)
					{
						Console.Error.WriteLine("error: multiple input files specified.");
						return 1;
					}

					inputFile = ExpandPath(args[i]);
					break;
			}
		}

		if (inputFile == null)
		{
			Console.Error.WriteLine("error: no input file specified.");
			return 1;
		}

		if (outputFile == null)
		{
			outputFile = Path.ChangeExtension(inputFile, ".bin");
		}

		if (!File.Exists(inputFile))
		{
			Console.Error.WriteLine($"error: cannot find '{inputFile}'.");
			return 1;
		}

		Stopwatch stopwatch = Stopwatch.StartNew();

		string source = File.ReadAllText(inputFile);

		Result result = new();

		Lexer lexer = new(source, inputFile);
		Result<List<Token>> tokenizeResult = lexer.Tokenize();
		result.Combine(tokenizeResult);

		Parser parser = new(tokenizeResult.ResultObject);
		Result<List<Statement>> parseResult = parser.Parse();
		result.Combine(parseResult);

		Assembler assembler = new(parseResult.ResultObject);
		Result<List<byte>> assembleResult = assembler.Assemble();
		result.Combine(assembleResult);

		stopwatch.Stop();

		if (result.IsSuccess)
		{
			File.WriteAllBytes(outputFile, assembleResult.ResultObject.ToArray());

			if (symbolFile != null)
			{
				List<string> lines = [];

				foreach (var kvp in assembler.SymbolTable.OrderBy(x => x.Value.Value))
				{
					Symbol symbol = kvp.Value;
					if (symbol.Type == Constants.SymbolType.Label)
					{
						lines.Add($"{symbol.Name}|{symbol.Type}|{symbol.Value:X8}");
					}
					else
					{
						lines.Add($"{symbol.Name}|{symbol.Type}|{symbol.Value}");
					}
				}

				File.WriteAllLines(symbolFile, lines);
			}

			if (listingFile != null)
			{
				// TODO: Generate listing file.
				Console.WriteLine("Listing file generation not yet implemented.");
			}

			Console.WriteLine(
				$"Assembled {assembleResult.ResultObject.Count} bytes in {stopwatch.Elapsed.TotalSeconds:N3} seconds."
			);
		}

		if (result.Warnings.Any())
		{
			Console.WriteLine();
			Console.WriteLine("Warnings:");

			foreach (var warning in result.Warnings)
				Console.WriteLine($"  {warning}");
		}

		if (result.Errors.Any())
		{
			Console.WriteLine();
			Console.WriteLine("Errors:");

			foreach (var error in result.Errors)
				Console.WriteLine($"  {error}");
		}

		return result.IsSuccess ? 0 : 1;
	}

	private static string ExpandPath(string path)
	{
		path = path.Trim('"');

		if (path.StartsWith('~'))
		{
			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			path = Path.Combine(home, path[1..]);
		}

		return path;
	}

	private static void PrintUsage()
	{
		Console.WriteLine("""
            Usage:
              im800asm [options] <input>

            Options:
              -o <file>      Output binary file (default: <input>.bin)
              -s <file>      Write symbol table
              -l <file>      Write listing file (stub)
              --test <file>  Run assembler tests
              -h, --help     Show this help
            """);
	}

	private static void RunTests(string fileName)
	{
		List<TestCase> testCases = Tester.ParseTestCases(fileName);
		Tester.Test(testCases);
	}
}