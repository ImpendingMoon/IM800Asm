namespace IM800Asm;

internal class SourceContext
{
	public SourceContext(string filePath, string source)
	{
		Source = source;
		Position = 0;
		Location = new(filePath, 1, 1);
	}

	public string Source { get; set; }
	public int Position { get; set; }
	// Not a property because Location is a struct we want to directly access
	public Location Location;
}