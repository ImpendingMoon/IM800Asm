using IM800Asm.Core;

namespace IM800Asm.Preprocess;

internal class Preprocessor
{
	private readonly HashSet<string> _activeIncludes = [];
	private Stack<SourceContext> _contextStack = [];
	private readonly SourceContext _currentContext;
	private readonly List<SourceLine> _processedLines = [];

	public Preprocessor(string filePath, string[] source)
	{
		_currentContext = new SourceContext(filePath, source);
		_activeIncludes.Add(filePath);
	}

	public Result<List<SourceLine>> Preprocess()
	{
		Result<List<SourceLine>> result = new(_processedLines);

		for (int i = 0; i < _currentContext.Source.Length; i++)
		{
			string line = _currentContext.Source[i];
			Location location = new(_currentContext.Location.FilePath, i, 0);
			_processedLines.Add(new SourceLine(location, line));
		}

		return result;
	}
}
