namespace IM800Asm.Core;

internal class Symbol(SourceLocation sourceLocation, Constants.SymbolType type, string name, long value)
{
	public SourceLocation SourceLocation { get; set; } = sourceLocation;
	public Constants.SymbolType Type { get; set; } = type;
	public string Name { get; set; } = name;
	public long Value { get; set; } = value;
}
