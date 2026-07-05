using System.Reflection.Metadata.Ecma335;

namespace IM800Asm;

internal class Token
{
	public int Line { get; set; }
	public int Column { get; set; }

	public virtual string ToShortString()
	{
		return ToString() ?? string.Empty;
	}
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

	public override string ToShortString()
	{
		return Type switch
		{
			Constants.TokenType.Colon => ":",
			Constants.TokenType.Comma => ",",
			Constants.TokenType.LParen => "(",
			Constants.TokenType.RParen => ")",
			Constants.TokenType.LBracket => "[",
			Constants.TokenType.RBracket => "]",
			Constants.TokenType.Plus => "+",
			Constants.TokenType.Minus => "-",
			Constants.TokenType.Star => "*",
			Constants.TokenType.Slash => "/",
			Constants.TokenType.Percent => "%",
			Constants.TokenType.ShiftLeft => "<<",
			Constants.TokenType.ShiftRight => ">>",
			Constants.TokenType.Ampersand => "&",
			Constants.TokenType.Pipe => "|",
			Constants.TokenType.Caret => "^",
			Constants.TokenType.Tilde => "~",
			Constants.TokenType.Equal => "==",
			Constants.TokenType.NotEqual => "!=",
			Constants.TokenType.Greater => ">",
			Constants.TokenType.Less => "<",
			Constants.TokenType.GreaterEqual => ">=",
			Constants.TokenType.LessEqual => "<=",
			Constants.TokenType.Exclamation => "!",
			Constants.TokenType.Dollar => "$",
			Constants.TokenType.NewLine => "\\n",
			Constants.TokenType.EndOfFile => "EOF",
			_ => Type.ToString(),
		};
	}

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

	public override string ToShortString()
	{
		return Lexeme;
	}

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

	public override string ToShortString()
	{
		return Lexeme;
	}

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

	public override string ToShortString()
	{
		return Lexeme;
	}

	public override string ToString()
	{
		return $"{Line}:{Column}:\tString: {Lexeme}";
	}
}