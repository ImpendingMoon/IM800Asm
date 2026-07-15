using IM800Asm.Core;

namespace IM800Asm.Lexing;

internal class Token
{
	public Location Location { get; set; } = new(string.Empty, 0, 0);

	public virtual string ToShortString()
	{
		return ToString() ?? string.Empty;
	}
}

internal class SymbolToken : Token
{
	public SymbolToken(Location location, Constants.TokenType type)
	{
		Location = location;
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
		return $"{Location} Symbol: {Type}";
	}
}

internal class IdentifierToken : Token
{
	public IdentifierToken(Location location, string lexeme)
	{
		Location = location;
		Lexeme = lexeme;
	}

	public string Lexeme { get; set; }

	public override string ToShortString()
	{
		return Lexeme;
	}

	public override string ToString()
	{
		return $"{Location} Identifier: {Lexeme}";
	}
}

internal class NumberToken : Token
{
	public NumberToken(Location location, string lexeme, long value)
	{
		Location = location;
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
		return $"{Location} Number: {Lexeme} (0x{Value:X})";
	}
}

internal class StringToken : Token
{
	public StringToken(Location location, string lexeme, List<byte> stringData)
	{
		Location = location;
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
		return $"{Location} String: {Lexeme}";
	}
}