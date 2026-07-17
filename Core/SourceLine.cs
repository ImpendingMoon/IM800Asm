namespace IM800Asm.Core;

internal class SourceLine(Location location, string source)
{
	public Location Location { get; set; } = location;
	public string Source { get; set; } = source;
	public static SourceLine Empty => new(new Location(string.Empty, 0, 0), string.Empty);
}
