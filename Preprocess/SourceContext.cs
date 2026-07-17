using IM800Asm.Core;

namespace IM800Asm.Preprocess;

internal class SourceContext(string filePath, string[] source)
{
	public string[] Source { get; set; } = source;
	public SourceLocation SourceLocation { get; set; } = new(filePath, 0, 0);
}
