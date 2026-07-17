using System.Text.Json;
using System.Text.Json.Serialization;
using IM800Asm.Assembly;
using IM800Asm.Core;
using IM800Asm.Lexing;
using IM800Asm.Parsing;

namespace IM800Asm.Testing;

internal class TestCase
{
	public string Name { get; set; } = string.Empty;
	public string[] Source { get; set; } = [];
	public string ExpectedOutputHex { get; set; } = string.Empty;

	[JsonIgnore] public byte[] ExpectedOutput => Convert.FromHexString(ExpectedOutputHex);
}

internal class TestResult
{
	public TestResult(TestCase testCase)
	{
		Name = testCase.Name;
		Source = testCase.Source;
		ExpectedOutput = testCase.ExpectedOutput;
		ActualOutput = [];
		Result = new Result();
	}

	public string Name { get; set; }
	public string[] Source { get; set; }
	public byte[] ExpectedOutput { get; set; }
	public List<byte> ActualOutput { get; set; }
	public Result Result { get; set; }
	public bool Passed => ExpectedOutput.SequenceEqual(ActualOutput) && Result.IsSuccess;
}

internal class Tester
{
	public static List<TestCase> ParseTestCases(string fileName)
	{
		string json = File.ReadAllText(fileName);

		var testCases = JsonSerializer.Deserialize<List<TestCase>>(json);

		if (testCases is null)
		{
			throw new InvalidOperationException("Failed to parse test file.");
		}

		return testCases;
	}

	public static void Test(List<TestCase> testCases)
	{
		foreach (TestCase testCase in testCases)
		{
			TestResult testResult = new(testCase);

			var lexer = new Lexer(testCase.Name, testCase.Source);
			Result<List<Token>> lexerResult = lexer.Tokenize();

			if (!testResult.Result.IsSuccess)
			{
				PrintResult(testResult);
				continue;
			}

			var parser = new Parser(lexerResult.ResultObject);
			Result<List<Statement>> parserResult = parser.Parse();
			testResult.Result.Combine(parserResult);

			if (!testResult.Result.IsSuccess)
			{
				PrintResult(testResult);
				continue;
			}

			var assembler = new Assembler(parserResult.ResultObject);
			Result<List<byte>> assemblerResult = assembler.Assemble();
			testResult.Result.Combine(assemblerResult);

			testResult.ActualOutput = assemblerResult.ResultObject;

			PrintResult(testResult);
		}
	}

	private static string FormatBytes(IEnumerable<byte> bytes)
	{
		return Convert.ToHexString([.. bytes]);
	}

	private static void PrintResult(TestResult testResult)
	{
		if (testResult.Passed)
		{
			Console.WriteLine($"PASS: {testResult.Name}");
		}
		else
		{
			Console.WriteLine($"FAIL: {testResult.Name}");

			Console.WriteLine($"\tEXPECTED: {FormatBytes(testResult.ExpectedOutput)}");
			Console.WriteLine($"\tACTUAL  : {FormatBytes(testResult.ActualOutput)}");

			foreach (Result.Error warning in testResult.Result.Warnings)
			{
				Console.WriteLine($"\tWARN: \"{warning}\"");
			}

			foreach (Result.Error error in testResult.Result.Errors)
			{
				Console.WriteLine($"\tERROR: \"{error}\"");
			}
		}
	}
}
