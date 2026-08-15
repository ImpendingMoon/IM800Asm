namespace IM800Asm.Core;

internal class SourceLine
{
	public SourceLine(string filePath, int line, string text)
	{
		FilePath = filePath;
		Line = line;
		Text = text;
	}

	public string FilePath { get; set; }
	public int Line { get; set; }
	public string Text { get; set; }
}
