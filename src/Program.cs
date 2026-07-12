using System.Diagnostics;
using System.Text;

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

		string[] source = File.ReadAllLines(inputFile);

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
				WriteSymbolFile(symbolFile, assembler.SymbolTable);
			}

			if (listingFile != null)
			{
				WriteListingFile(listingFile, lexer.SourceLines, parseResult.ResultObject, assembleResult.ResultObject);
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
			string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			path = path.Replace("~", userFolder);
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

	private static void WriteSymbolFile(string filePath, IReadOnlyDictionary<string, Symbol> symbolTable)
	{
		List<string> lines = [];

		foreach (var kvp in symbolTable.OrderBy(x => x.Value.Value))
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

		File.WriteAllLines(filePath, lines);
	}

	private static void WriteListingFile(
		string filePath,
		List<SourceLine> sourceLines,
		List<Statement> statements,
		List<byte> output
	)
	{
		// For every source line, find statements on that line
		// - For every statement on that line, get all bytes from those statements
		// - Print out address, bytes, source line
		// - If no bytes, just print out source line
		List<ListingEntry> listingEntries = [];
		int currentStatement = 0;

		// Both arrays must be sorted by line
		// But since INCLUDEs exist and lines may be out of order, everything has to be added in order at each stage

		int currentBaseAddress = 0;

		foreach (SourceLine sourceLine in sourceLines)
		{
			List<byte> bytes = [];

			bool hasAddress = false;

			while (currentStatement < statements.Count)
			{
				Statement statement = statements[currentStatement];

				// We've reached statements for a later source line.
				if (statement.Location.Line > sourceLine.Location.Line)
				{
					break;
				}

				// First statement on this source line determines the address.
				if (!hasAddress)
				{
					currentBaseAddress = (int)statement.MeasuredLocationCounter;
					hasAddress = true;
				}

				int startIndex = (int)statement.FileOffset;
				int endIndex = startIndex + (int)statement.Length;
				bytes.AddRange(output[startIndex..endIndex]);

				currentStatement++;
			}

			listingEntries.Add(new ListingEntry(currentBaseAddress, sourceLine, bytes));
		}

		StringBuilder sb = new();

		foreach (ListingEntry entry in listingEntries)
		{

			string address = entry.BaseAddress.ToString("X8");

			string primaryByteLine = string.Empty;
			List<string> additionalByteLines = [];

			const int bytesPerLine = 8;
			// 3 chars per byte, 2 chars for end byte (no space after)
			const int padAmount = bytesPerLine * 3 - 1;

			int currentByte = 0;
			while (currentByte < entry.Bytes.Count)
			{
				string byteLine = string.Join(
					' ',
					entry.Bytes
						.Skip(currentByte)
						.Take(bytesPerLine)
						.Select(x => x.ToString("X2"))
				);
				byteLine = byteLine.PadRight(padAmount);

				if (currentByte == 0)
				{
					primaryByteLine = byteLine;
				}
				else
				{
					// Pad out space not taken by address
					byteLine = byteLine.PadLeft(padAmount + 10);
					additionalByteLines.Add(byteLine);
				}

				currentByte += bytesPerLine;
			}

			if (primaryByteLine.Length == 0)
			{
				primaryByteLine = new string(' ', padAmount);
			}

			sb.Append(address);
			sb.Append(": ");
			sb.Append(primaryByteLine);
			sb.Append(" ");
			sb.AppendLine(entry.SourceLine.Source);
			foreach (string additionalLine in additionalByteLines)
			{
				sb.AppendLine(additionalLine);
			}
		}

		File.WriteAllText(filePath, sb.ToString());
	}

	private class ListingEntry
	{
		public ListingEntry(int baseAddress, SourceLine sourceLine, List<byte> bytes)
		{
			BaseAddress = baseAddress;
			SourceLine = sourceLine;
			Bytes = bytes;
		}

		public int BaseAddress { get; set; }
		public SourceLine SourceLine { get; set; }
		public List<byte> Bytes { get; set; }
	}

	private static void RunTests(string fileName)
	{
		List<TestCase> testCases = Tester.ParseTestCases(fileName);
		Tester.Test(testCases);
	}
}