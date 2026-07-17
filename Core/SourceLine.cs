namespace IM800Asm.Core;

internal class SourceLine(SourceLocation sourceLocation, string source)
{
	public SourceLocation SourceLocation { get; set; } = sourceLocation;
	public string Source { get; set; } = source;
	public static SourceLine Empty => new(new SourceLocation(string.Empty, 0, 0), string.Empty);
}
