using System.Reflection.Metadata.Ecma335;

namespace IM800Asm;

internal class Token
{
	public int Line { get; set; }
	public int Column { get; set; }
}

internal class SymbolToken : Token
{
	public SymbolToken(int line, int column, Constants.TokenType type)
	{
		Line = line;
		Column = column;
		Type = type;
	}

	public Constants.TokenType Type { get; set; }

	public override string ToString()
	{
		return $"{Line}:{Column}:\tSymbol: {Type}";
	}
}

internal class IdentifierToken : Token
{
	public IdentifierToken(int line, int column, string lexeme)
	{
		Line = line;
		Column = column;
		Lexeme = lexeme;
	}

	public string Lexeme { get; set; }

	public override string ToString()
	{
		return $"{Line}:{Column}:\tIdentifier: {Lexeme}";
	}
}

internal class NumberToken : Token
{
	public NumberToken(int line, int column, string lexeme, long value)
	{
		Line = line;
		Column = column;
		Lexeme = lexeme;
		Value = value;
	}

	public string Lexeme { get; set; }
	public long Value { get; set; }

	public override string ToString()
	{
		return $"{Line}:{Column}:\tNumber: {Lexeme} (0x{Value:X})";
	}
}

internal class StringToken : Token
{
	public StringToken(int line, int column, string lexeme, List<byte> stringData)
	{
		Line = line;
		Column = column;
		Lexeme = lexeme;
		StringData = stringData;
	}

	public string Lexeme { get; set; }
	public List<byte> StringData { get; set; }

	public override string ToString()
	{
		return $"{Line}:{Column}:\tString: {Lexeme}";
	}
}