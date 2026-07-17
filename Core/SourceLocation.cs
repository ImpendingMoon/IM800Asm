namespace IM800Asm.Core;

public struct SourceLocation
{
	public SourceLocation(string filePath, int line, int column)
	{
		FilePath = filePath;
		Line = line;
		Column = column;
	}

	public readonly string FilePath;
	public int Column;
	public int Line;

	public override string ToString()
	{
		// Used for indexing (0-indexed) and printing (1-indexed)
		return $"{FilePath}:{Line + 1}:{Column + 1}:";
	}
}
