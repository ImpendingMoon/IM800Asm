namespace IM800Asm;

internal class SourceContext
{
	public SourceContext(string filePath, string[] source)
	{
		Source = source;
		Location = new(filePath, 0, 0);
	}

	public string[] Source { get; set; }
	// Not a property because Location is a struct we want to directly access
	public Location Location;
}