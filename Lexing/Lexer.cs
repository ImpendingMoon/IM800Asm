using System.Diagnostics;
using System.Globalization;
using System.Text;
using IM800Asm.Core;

namespace IM800Asm.Lexing;

internal class Lexer(string fileName, string[] sourceLines)
{
	private readonly List<Token> _tokens = [];
	private SourceLocation _sourceLocation = new(fileName, 0, 0);

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

				if (tokenResult.ResultObject is SymbolToken { Type: Constants.TokenType.EndOfFile })
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
			var token = new SymbolToken(_sourceLocation, Constants.TokenType.EndOfFile);
			result.ResultObject = token;
		}
		else if (IsNewLine(c))
		{
			var token = new SymbolToken(_sourceLocation, Constants.TokenType.NewLine);
			ConsumeNewLine();
			result.ResultObject = token;
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
			result.AddError(
				_sourceLocation,
				Constants.ErrorCode.UnexpectedCharacter,
				$"unexpected character {c}"
			);
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
		switch (c, n)
		{
			case ('<', '<'):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.ShiftLeft);
				matchedTwo = true;
				break;
			case ('>', '>'):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.ShiftRight);
				matchedTwo = true;
				break;
			case ('=', '='):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Equal);
				matchedTwo = true;
				break;
			case ('!', '='):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.NotEqual);
				matchedTwo = true;
				break;
			case ('>', '='):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.GreaterEqual);
				matchedTwo = true;
				break;
			case ('<', '='):
				token = new SymbolToken(_sourceLocation, Constants.TokenType.LessEqual);
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
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Comma);
				matchedOne = true;
				break;
			case ':':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Colon);
				matchedOne = true;
				break;
			case '(':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.LParen);
				matchedOne = true;
				break;
			case ')':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.RParen);
				matchedOne = true;
				break;
			case '[':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.LBracket);
				matchedOne = true;
				break;
			case ']':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.RBracket);
				matchedOne = true;
				break;
			case '+':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Plus);
				matchedOne = true;
				break;
			case '-':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Minus);
				matchedOne = true;
				break;
			case '*':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Star);
				matchedOne = true;
				break;
			case '/':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Slash);
				matchedOne = true;
				break;
			case '%':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Percent);
				matchedOne = true;
				break;
			case '&':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Ampersand);
				matchedOne = true;
				break;
			case '|':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Pipe);
				matchedOne = true;
				break;
			case '^':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Caret);
				matchedOne = true;
				break;
			case '~':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Tilde);
				matchedOne = true;
				break;
			case '>':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Greater);
				matchedOne = true;
				break;
			case '<':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Less);
				matchedOne = true;
				break;
			case '!':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Exclamation);
				matchedOne = true;
				break;
			case '$':
				token = new SymbolToken(_sourceLocation, Constants.TokenType.Dollar);
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
		result = new Result<NumberToken?>(null);

		char c = Current();

		if (c != '\'')
		{
			return false;
		}

		int startColumn = _sourceLocation.Column;

		Advance();
		c = Current();

		if (c == '\'')
		{
			result.AddError(
				_sourceLocation,
				Constants.ErrorCode.EmptyCharacterLiteral,
				"expected character in character literal"
			);
			result.ResultObject = new NumberToken(_sourceLocation, "''", 0);
			Advance();
		}
		else
		{
			Result<long> parseResult = ParseCharInternal();
			result.Combine(parseResult);

			c = Current();

			if (c != '\'')
			{
				result.AddError(
					_sourceLocation,
					Constants.ErrorCode.UnterminatedCharacterLiteral,
					"expected end of character literal"
				);
			}
			else
			{
				Advance();
			}

			result.ResultObject = new NumberToken(
				_sourceLocation,
				sourceLines[_sourceLocation.Line][startColumn.._sourceLocation.Column],
				parseResult.ResultObject
			);
		}

		return true;
	}

	private bool TryParseStringLiteral(out Result<StringToken?> result)
	{
		result = new Result<StringToken?>(null);

		char c = Current();

		if (c != Constants.StringDelim)
		{
			return false;
		}

		List<byte> stringValue = [];

		SourceLocation startSourceLocation = _sourceLocation;

		Advance();
		c = Current();

		while (c != Constants.StringDelim)
		{
			if (IsNewLine(c) || c == '\0')
			{
				result.AddError(
					_sourceLocation,
					Constants.ErrorCode.UnterminatedStringLiteral,
					"expected end of string literal"
				);
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

		result.ResultObject = new StringToken(
			startSourceLocation,
			sourceLines[_sourceLocation.Line][startSourceLocation.Column.._sourceLocation.Column],
			stringValue
		);

		return true;
	}

	private bool TryParseNumberLiteral(out Result<NumberToken?> result)
	{
		result = new Result<NumberToken?>(null);

		char c = Current();

		if (!char.IsAsciiDigit(c))
		{
			return false;
		}

		SourceLocation startSourceLocation = _sourceLocation;

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
			result.AddError(
				startSourceLocation,
				Constants.ErrorCode.NumberLiteralTooLarge,
				$"number literal value too large \"{lexeme}\""
			);
		}
		catch (Exception)
		{
			result.AddError(
				startSourceLocation,
				Constants.ErrorCode.InvalidNumberLiteral,
				$"invalid number literal \"{lexeme}\""
			);
		}


		result.ResultObject = new NumberToken(startSourceLocation, lexeme, value);

		return true;
	}

	private bool TryParseIdentifier(out Result<IdentifierToken?> result)
	{
		result = new Result<IdentifierToken?>(null);

		char c = Current();

		if (!IsIdentifierStart(c))
		{
			return false;
		}

		int startColumn = _sourceLocation.Column;
		SourceLocation startSourceLocation = _sourceLocation;

		while (IsIdentifierChar(c))
		{
			Advance();
			c = Current();
		}

		result.ResultObject = new IdentifierToken(
			startSourceLocation,
			sourceLines[_sourceLocation.Line][startColumn.._sourceLocation.Column]
		);

		return true;
	}

	/// <summary>
	///     Converts a character or escape sequence to an integer value.
	///     Advances past the character.
	/// </summary>
	/// <returns>A result object with the parsed byte value</returns>
	private Result<long> ParseCharInternal()
	{
		Result<long> result = new(0);

		char c = Current();

		SourceLocation startSourceLocation = _sourceLocation;

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
							startSourceLocation,
							Constants.ErrorCode.InvalidEscapeSequence,
							"expected two hex characters for escaped hex value"
						);
						break;
					}

					Advance();
					char lower = Current();
					if (!char.IsAsciiHexDigit(lower))
					{
						result.AddError(
							startSourceLocation,
							Constants.ErrorCode.InvalidEscapeSequence,
							"expected two hex characters for escaped hex value"
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
						result.AddError(
							startSourceLocation,
							Constants.ErrorCode.InvalidEscapeSequence,
							$"invalid escaped hex value \"\\x{full}\""
						);
					}

					Advance();

					break;
				}
				default:
				{
					result.AddError(
						startSourceLocation,
						Constants.ErrorCode.InvalidEscapeSequence,
						$"invalid escape sequence \"\\{c}\""
					);
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
		_sourceLocation.Line++;
		_sourceLocation.Column = 0;
	}

	private char Current()
	{
		if (_sourceLocation.Line >= sourceLines.Length)
		{
			return '\0';
		}

		if (_sourceLocation.Column >= sourceLines[_sourceLocation.Line].Length)
		{
			return '\n';
		}

		return sourceLines[_sourceLocation.Line][_sourceLocation.Column];
	}

	private char Next()
	{
		if (_sourceLocation.Line >= sourceLines.Length)
		{
			return '\0';
		}

		if (_sourceLocation.Column + 1 >= sourceLines[_sourceLocation.Line].Length)
		{
			return '\n';
		}

		return sourceLines[_sourceLocation.Line][_sourceLocation.Column + 1];
	}

	private void Advance(int count = 1)
	{
		_sourceLocation.Column += count;
	}
}
