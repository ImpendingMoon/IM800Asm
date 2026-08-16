namespace IM800Asm.Preprocessing;

internal class SourceContext
{
	public SourceContext(string fileName, string[] sourceLines)
	{
		FilePath = fileName;
		CurrentLine = 0;
		SourceLines = sourceLines;
	}

	public string FilePath { get; set; }
	public int CurrentLine { get; set; }
	public string[] SourceLines { get; set; }
}
