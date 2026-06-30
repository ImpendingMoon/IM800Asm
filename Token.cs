namespace IM800Asm;

internal class Token
{
	public Token(int line, int column, Constants.TokenType type)
	{
		Line = line;
		Column = column;
		Type = type;
		Lexeme = string.Empty;
		StringValue = [];
	}

	public Token(int line, int column, Constants.TokenType type, string text)
	{
		Line = line;
		Column = column;
		Type = type;
		Lexeme = text;
		StringValue = [];
	}

	public Token(int line, int column, Constants.TokenType type, string lexeme, long value)
	{
		Line = line;
		Column = column;
		Lexeme = lexeme;
		Type = type;
		Value = value;
		StringValue = [];
	}

	public Token(int line, int column, Constants.TokenType type, string text, List<byte> stringValue)
	{
		Line = line;
		Column = column;
		Lexeme = text;
		Type = type;
		StringValue = stringValue;
	}

	public Constants.TokenType Type { get; set; }
	public int Line { get; set; }
	public int Column { get; set; }

	/// <summary>
	/// Raw text from the source code
	/// </summary>
	public string Lexeme { get; set; }

	/// <summary>
	/// Value of a string literal
	/// </summary>
	public List<byte> StringValue { get; set; }

	/// <summary>
	/// Value of a numeric literal
	/// </summary>
	public long Value { get; set; }

	public override string ToString()
	{
		return Type switch
		{
			Constants.TokenType.Identifier => $"{Line}:{Column}:\t{Type}: Lexeme: {Lexeme}",
			Constants.TokenType.String => $"{Line}:{Column}:\t{Type}: Text: {Lexeme}",
			Constants.TokenType.Number => $"{Line}:{Column}:\t{Type}: Lexeme: {Lexeme}, Value: 0x{Value:X}",
			_ => $"{Line}:{Column}:\t{Type}",
		};
	}
}
