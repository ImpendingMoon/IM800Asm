namespace IM800Asm.Core;

internal class Location
{
	public Location(string filePath, int line, int column)
	{
		FilePath = filePath;
		Line = line;
		Column = column;
	}

	public string FilePath;
	public int Line;
	public int Column;

	public override string ToString()
	{
		// Used for indexing (0-indexed) and printing (1-indexed)
		return $"{FilePath}:{Line + 1}:{Column + 1}:";
	}
}