namespace IM800Asm;

internal struct Location
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
		return $"{FilePath}:{Line}:{Column}:";
	}
}