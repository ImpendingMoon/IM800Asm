namespace IM800Asm.Core;

internal class Location(string filePath, int line, int column)
{
	public readonly string FilePath = filePath;
	public int Column = column;
	public int Line = line;

	public override string ToString()
	{
		// Used for indexing (0-indexed) and printing (1-indexed)
		return $"{FilePath}:{Line + 1}:{Column + 1}:";
	}
}
