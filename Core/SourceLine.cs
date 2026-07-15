namespace IM800Asm.Core;

internal class SourceLine
{
	public SourceLine(Location location, string source)
	{
		Location = location;
		Source = source;
	}

	public Location Location { get; set; }
	public string Source { get; set; }
	public static SourceLine Empty => new(new(string.Empty, 0, 0), string.Empty);
}