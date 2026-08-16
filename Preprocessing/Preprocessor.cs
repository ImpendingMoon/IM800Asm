using IM800Asm.Core;
using System.Text.RegularExpressions;

namespace IM800Asm.Preprocessing;

internal class Preprocessor
{
	private Stack<SourceContext> _contextStack = [];
	private SourceContext _currentContext;
	private HashSet<string> _activeIncludes = [];
	private List<SourceLine> _outputLines = [];

	public Preprocessor(string filePath, string[] sourceLines)
	{
		SourceContext initialContext = new(filePath, sourceLines);
		_activeIncludes.Add(filePath);
		_currentContext = initialContext;
	}

	public Result<List<SourceLine>> Preprocess()
	{
		Result<List<SourceLine>> result = new(_outputLines);

		while (true)
		{
			// At the end of the current context. Pop to the next one or end if no more remain.
			if (_currentContext.CurrentLine >= _currentContext.SourceLines.Length)
			{
				if (_contextStack.Count == 0)
				{
					break;
				}
				_activeIncludes.Remove(_currentContext.FilePath);
				_currentContext = _contextStack.Pop();
			}
			else
			{
				string sourceText = _currentContext.SourceLines[_currentContext.CurrentLine];

				// Check for preprocessor directives
				if (sourceText.Contains("%include", StringComparison.OrdinalIgnoreCase))
				{
					// Pre-increment to skip the line with %include
					_currentContext.CurrentLine++;
					Result includeResult = ProcessIncludeStatement(sourceText);
					result.Combine(includeResult);
				}
				else
				{
					// No directives, add line as-is
					SourceLine identityLine = new(_currentContext.FilePath, _currentContext.CurrentLine, sourceText);
					_outputLines.Add(identityLine);
					_currentContext.CurrentLine++;
				}
			}
		}

		return result;
	}

	public Result ProcessIncludeStatement(string sourceText)
	{
		Result result = new();

		// Line - 1 because we've pre-incremented it to skip the line with %include
		SourceLocation sourceLocation = new(_currentContext.FilePath, _currentContext.CurrentLine - 1, 0);
		MatchCollection stringMatches = Regex.Matches(sourceText, "\".*?\"");

		if (stringMatches.Count == 0)
		{
			result.AddError(sourceLocation, Constants.ErrorCode.ExpectedOperand, "expected string literal after %include directive");
		}
		else if (stringMatches.Count > 1)
		{
			result.AddError(sourceLocation, Constants.ErrorCode.UnexpectedOperand, "expected one string literal after %include directive");
		}
		else
		{
			string filePath = stringMatches[0].Value.Trim('"');

			if (!Path.IsPathRooted(filePath))
			{
				// Get the full file path relative to the current file
				string baseDir = Path.GetDirectoryName(_currentContext.FilePath) ?? string.Empty;
				filePath = Path.GetFullPath(Path.Combine(baseDir, filePath));
			}

			if (!File.Exists(filePath))
			{
				result.AddError(sourceLocation, Constants.ErrorCode.FileNotFound, $"could not find the file {filePath}");
				return result;
			}

			if (_activeIncludes.Contains(filePath))
			{
				result.AddError(sourceLocation, Constants.ErrorCode.CircularDependency, $"circular include of file {filePath}");
				return result;
			}

			string[] sourceLines;
			try
			{
				sourceLines = File.ReadAllLines(filePath);
			}
			catch (Exception ex)
			{
				result.AddError(sourceLocation, Constants.ErrorCode.Unknown, $"failed to read the included file \"{filePath}\": {ex.Message}");
				return result;
			}

			// Save current context, add new context
			_contextStack.Push(_currentContext);
			SourceContext newContext = new(filePath, sourceLines);
			_currentContext = newContext;
			_activeIncludes.Add(filePath);
		}

		return result;
	}
}
