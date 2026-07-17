namespace IM800Asm.Core;

internal class Symbol(Constants.SymbolType type, string name, long value)
{
	public Constants.SymbolType Type { get; set; } = type;
	public string Name { get; set; } = name;
	public long Value { get; set; } = value;
}
