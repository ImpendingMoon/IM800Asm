using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace IM800Asm;

internal class Lexer
{
	private string _source = string.Empty;
	private int _position = 0;
	private int _line = 1;
	private int _column = 1;
	private List<Token> _tokens = [];

	public Lexer(string source)
	{
		_source = source;
	}

	public Result<List<Token>> Tokenize()
	{
		Result<List<Token>> result = new(_tokens);

		while (true)
		{
			Result<Token?> tokenResult = NextToken();
			result.Combine(tokenResult);

			if (tokenResult.ResultObject is not null)
			{
				_tokens.Add(tokenResult.ResultObject);

				if (tokenResult.ResultObject is SymbolToken t && t.Type == Constants.TokenType.EndOfFile)
				{
					break;
				}
			}
		}

		return result;
	}

	private Result<Token?> NextToken()
	{
		Result<Token?> result = new(null);

		SkipWhiteSpace();

		char c = Current();

		if (c == '\0')
		{
			SymbolToken token = new(_line, _column, Constants.TokenType.EndOfFile);
			result.ResultObject = token;
		}
		else if (IsNewLine(c))
		{
			SymbolToken token = new(_line, _column, Constants.TokenType.NewLine);
			ConsumeNewLine();

			// Don't care about repeated newlines
			if (
				_tokens.Count > 0
				&& _tokens[^1] is SymbolToken t
				&& t.Type != Constants.TokenType.NewLine
			)
			{
				result.ResultObject = token;
			}
		}
		else if (c == Constants.CommentChar)
		{
			SkipToNewLine();
			// Leave null, end may be newline or end of file, next call to NextToken will catch
		}
		else if (TryParseSymbol(out SymbolToken? symbolToken))
		{
			Debug.Assert(symbolToken is not null);
			result.ResultObject = symbolToken;
		}
		else if (TryParseCharLiteral(out Result<NumberToken?> charResult))
		{
			Debug.Assert(charResult.ResultObject is not null);
			result.Combine(charResult);
			result.ResultObject = charResult.ResultObject;
		}
		else if (TryParseStringLiteral(out Result<StringToken?> stringResult))
		{
			Debug.Assert(stringResult.ResultObject is not null);
			result.Combine(stringResult);
			result.ResultObject = stringResult.ResultObject;
		}
		else if (TryParseNumberLiteral(out Result<NumberToken?> numberResult))
		{
			Debug.Assert(numberResult.ResultObject is not null);
			result.Combine(numberResult);
			result.ResultObject = numberResult.ResultObject;
		}
		else if (TryParseIdentifier(out Result<IdentifierToken?> identifierResult))
		{
			Debug.Assert(identifierResult.ResultObject is not null);
			result.Combine(identifierResult);
			result.ResultObject = identifierResult.ResultObject;
		}
		else
		{
			result.AddError("Lexer", $"{_line}:{_column}:\tunexpected character {c}");
			Advance();
		}

		return result;
	}

	private void SkipWhiteSpace()
	{
		char c = Current();

		while (char.IsWhiteSpace(c))
		{
			if (IsNewLine(c))
			{
				break;
			}

			Advance();
			c = Current();
		}
	}

	private void SkipToNewLine()
	{
		char c = Current();

		while (!IsNewLine(c) && c != '\0')
		{
			Advance();
			c = Current();
		}
	}

	private bool TryParseSymbol(out SymbolToken? token)
	{
		token = null;

		char c = Current();
		char n = Next();

		bool matchedTwo = false;

		// Try as a two-char token first
		switch ((c, n))
		{
			case ('<', '<'):
				token = new(_line, _column, Constants.TokenType.ShiftLeft);
				matchedTwo = true;
				break;
			case ('>', '>'):
				token = new(_line, _column, Constants.TokenType.ShiftRight);
				matchedTwo = true;
				break;
			case ('=', '='):
				token = new(_line, _column, Constants.TokenType.Equal);
				matchedTwo = true;
				break;
			case ('!', '='):
				token = new(_line, _column, Constants.TokenType.NotEqual);
				matchedTwo = true;
				break;
			case ('>', '='):
				token = new(_line, _column, Constants.TokenType.GreaterEqual);
				matchedTwo = true;
				break;
			case ('<', '='):
				token = new(_line, _column, Constants.TokenType.LessEqual);
				matchedTwo = true;
				break;
		}

		if (matchedTwo)
		{
			Advance(2);
			return true;
		}

		bool matchedOne = false;

		switch (c)
		{
			case ',':
				token = new(_line, _column, Constants.TokenType.Comma);
				matchedOne = true;
				break;
			case ':':
				token = new(_line, _column, Constants.TokenType.Colon);
				matchedOne = true;
				break;
			case '(':
				token = new(_line, _column, Constants.TokenType.LParen);
				matchedOne = true;
				break;
			case ')':
				token = new(_line, _column, Constants.TokenType.RParen);
				matchedOne = true;
				break;
			case '[':
				token = new(_line, _column, Constants.TokenType.LBracket);
				matchedOne = true;
				break;
			case ']':
				token = new(_line, _column, Constants.TokenType.RBracket);
				matchedOne = true;
				break;
			case '+':
				token = new(_line, _column, Constants.TokenType.Plus);
				matchedOne = true;
				break;
			case '-':
				token = new(_line, _column, Constants.TokenType.Minus);
				matchedOne = true;
				break;
			case '*':
				token = new(_line, _column, Constants.TokenType.Star);
				matchedOne = true;
				break;
			case '/':
				token = new(_line, _column, Constants.TokenType.Slash);
				matchedOne = true;
				break;
			case '%':
				token = new(_line, _column, Constants.TokenType.Percent);
				matchedOne = true;
				break;
			case '&':
				token = new(_line, _column, Constants.TokenType.Ampersand);
				matchedOne = true;
				break;
			case '|':
				token = new(_line, _column, Constants.TokenType.Pipe);
				matchedOne = true;
				break;
			case '^':
				token = new(_line, _column, Constants.TokenType.Caret);
				matchedOne = true;
				break;
			case '~':
				token = new(_line, _column, Constants.TokenType.Tilde);
				matchedOne = true;
				break;
			case '>':
				token = new(_line, _column, Constants.TokenType.Greater);
				matchedOne = true;
				break;
			case '<':
				token = new(_line, _column, Constants.TokenType.Less);
				matchedOne = true;
				break;
			case '!':
				token = new(_line, _column, Constants.TokenType.Exclamation);
				matchedOne = true;
				break;
			case '$':
				token = new(_line, _column, Constants.TokenType.Dollar);
				matchedOne = true;
				break;
		}

		if (matchedOne)
		{
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseCharLiteral(out Result<NumberToken?> result)
	{
		result = new(null);

		char c = Current();

		if (c != '\'')
		{
			return false;
		}

		int start = _position;
		int startColumn = _column;

		Advance();
		c = Current();

		if (c == '\'')
		{
			result.AddError("Lexer", $"{_line}:{_column}:\texpected character in character literal");
			result.ResultObject = new(_line, _column, "''", 0);
			Advance();
		}
		else
		{
			Result<long> parseResult = ParseCharInternal();
			result.Combine(parseResult);

			c = Current();

			if (c != '\'')
			{
				result.AddError("Lexer", $"{_line}:{_column}:\texpected end of character literal");
			}
			else
			{
				Advance();
			}

			result.ResultObject = new(
				_line,
				startColumn,
				_source[start.._position],
				parseResult.ResultObject
			);
		}

		return true;
	}

	private bool TryParseStringLiteral(out Result<StringToken?> result)
	{
		result = new(null);

		char c = Current();

		if (c != Constants.StringDelim)
		{
			return false;
		}

		List<byte> stringValue = [];

		int start = _position;
		int startColumn = _column;

		Advance();
		c = Current();

		while (c != Constants.StringDelim)
		{
			if (IsNewLine(c) || c == '\0')
			{
				result.AddError("Lexer", $"{_line}:{_column}:\texpected end of string literal");
				break;
			}

			Result<long> parseResult = ParseCharInternal();
			result.Combine(parseResult);

			long value = parseResult.ResultObject;
			stringValue.Add((byte)value);

			// Source text can be UTF-16, handle two-byte char
			// Don't handle UTF-8. Only doing this because C# strings are UTF-16.
			if (value > byte.MaxValue)
			{
				stringValue.Add((byte)(value >> 8));
			}

			c = Current();
		}

		// Can break on newline/eof, only consume end quote
		if (c == '"')
		{
			Advance();
		}

		result.ResultObject = new(
			_line,
			startColumn,
			_source[start.._position],
			stringValue
		);

		return true;
	}

	private bool TryParseNumberLiteral(out Result<NumberToken?> result)
	{
		result = new(null);

		char c = Current();

		if (!char.IsAsciiDigit(c))
		{
			return false;
		}

		int start = _position;
		int startColumn = _column;

		StringBuilder sb = new();

		// Greedily consume full identifier
		while (IsIdentifierChar(c))
		{
			sb.Append(c);
			Advance();
			c = Current();
		}

		string lexeme = sb.ToString();
		string text = lexeme.ToLower();
		int radix = Constants.DecimalRadix;

		if (text.StartsWith(Constants.HexPrefix))
		{
			radix = Constants.HexRadix;
			text = text[2..];
		}
		else if (text.StartsWith(Constants.BinaryPrefix))
		{
			radix = Constants.BinaryRadix;
			text = text[2..];
		}
		else if (text.StartsWith(Constants.OctalPrefix))
		{
			radix = Constants.OctalRadix;
			text = text[2..];
		}
		else if (text.EndsWith(Constants.HexSuffix))
		{
			radix = Constants.HexRadix;
			text = text[..^1];
		}
		else if (text.EndsWith(Constants.BinarySuffix))
		{
			radix = Constants.BinaryRadix;
			text = text[..^1];
		}
		else if (text.EndsWith(Constants.OctalSuffix))
		{
			radix = Constants.OctalRadix;
			text = text[..^1];
		}

		long value = 0;
		try
		{
			value = Convert.ToInt64(text, radix);
		}
		catch (OverflowException)
		{
			result.AddError("Lexer", $"{_line}:{startColumn}:\tnumber literal value too large \"{lexeme}\"");
		}
		catch (Exception)
		{
			result.AddError("Lexer", $"{_line}:{startColumn}:\tinvalid number literal \"{lexeme}\"");
		}

		result.ResultObject = new(_line, startColumn, lexeme, value);

		return true;
	}

	private bool TryParseIdentifier(out Result<IdentifierToken?> result)
	{
		result = new(null);

		char c = Current();

		if (!IsIdentifierStart(c))
		{
			return false;
		}

		int start = _position;
		int startColumn = _column;

		while (IsIdentifierChar(c))
		{
			Advance();
			c = Current();
		}

		result.ResultObject = new(_line, startColumn, _source[start.._position]);

		return true;
	}

	/// <summary>
	/// Converts a character or escape sequence to an integer value.
	/// Advances past the character.
	/// </summary>
	/// <returns>A result object with the parsed byte value</returns>
	private Result<long> ParseCharInternal()
	{
		Result<long> result = new(0);

		char c = Current();

		// In case we need to print out an error message, column should be start of char
		int startColumn = _column;

		// Escape sequence
		if (c == '\\')
		{
			Advance();
			c = Current();

			switch (c)
			{
				case '0':
					result.ResultObject = 0;
					Advance();
					break;
				case 'a':
					result.ResultObject = '\a';
					Advance();
					break;
				case 'b':
					result.ResultObject = '\b';
					Advance();
					break;
				case 'e':
					result.ResultObject = '\e';
					Advance();
					break;
				case 'f':
					result.ResultObject = '\f';
					Advance();
					break;
				case 'n':
					result.ResultObject = '\n';
					Advance();
					break;
				case 'r':
					result.ResultObject = '\r';
					Advance();
					break;
				case 't':
					result.ResultObject = '\t';
					Advance();
					break;
				case 'v':
					result.ResultObject = '\v';
					Advance();
					break;
				case '\'':
					result.ResultObject = '\'';
					Advance();
					break;
				case '"':
					result.ResultObject = '"';
					Advance();
					break;
				case 'x':
				{
					// \xXX, where X is two hex chars
					Advance();
					char upper = Current();
					if (!char.IsAsciiHexDigit(upper))
					{
						result.AddError(
							"Lexer",
							$"{_line}:{startColumn}:\texpected two hex characters for escaped hex value"
						);
						break;
					}

					Advance();
					char lower = Current();
					if (!char.IsAsciiHexDigit(lower))
					{
						result.AddError(
							"Lexer",
							$"{_line}:{startColumn}:\texpected two hex characters for escaped hex value"
						);
						break;
					}

					string full = $"{upper}{lower}";

					if (long.TryParse(full, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long value))
					{
						result.ResultObject = value;
					}
					else
					{
						result.AddError("Lexer", $"{_line}:{startColumn}:\tinvalid escaped hex value \"\\x{full}\"");
					}

					Advance();

					break;
				}
				default:
				{
					result.AddError("Lexer", $"{_line}:{startColumn}:\tinvalid escape sequence \"\\{c}\"");
					Advance();
					break;
				}
			}
		}
		else
		{
			result.ResultObject = c;
			Advance();
		}

		return result;
	}

	private static bool IsNewLine(char c)
	{
		return c is '\r' or '\n';
	}

	private static bool IsIdentifierStart(char c)
	{
		return char.IsAsciiLetter(c) || c is '.' or '_';
	}

	private static bool IsIdentifierChar(char c)
	{
		return char.IsAsciiLetterOrDigit(c) || c is '.' or '_';
	}

	private void ConsumeNewLine()
	{
		char c = Current();

		if (c == '\r')
		{
			Advance();
			c = Current();
		}

		if (c == '\n')
		{
			Advance();
		}

		_line++;
		_column = 1;
	}

	private char Current()
	{
		return _position >= _source.Length ? '\0' : _source[_position];
	}

	private char Next()
	{
		return _position + 1 >= _source.Length ? '\0' : _source[_position + 1];
	}

	private void Advance(int count = 1)
	{
		_position += count;
		_column += count;
	}
}
