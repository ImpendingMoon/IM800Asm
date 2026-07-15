using IM800Asm.Core;

namespace IM800Asm.Preprocess;

internal class SourceContext
{
	public SourceContext(string filePath, string[] source)
	{
		Source = source;
		Location = new(filePath, 0, 0);
	}

	public string[] Source { get; set; }
	public Location Location { get; set; }
}