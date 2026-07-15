using System.Diagnostics;
using System.Globalization;
using System.Text;
using IM800Asm.Core;

namespace IM800Asm.Lexing;

internal class Lexer
{
	private Stack<SourceContext> _contextStack = [];
	private HashSet<string> _activeIncludes = [];
	private SourceContext _currentContext;
	private List<Token> _tokens = [];
	private List<SourceLine> _sourceLines = [];
	public List<SourceLine> SourceLines => _sourceLines;

	public Lexer(string[] source, string filePath)
	{
		_currentContext = new(filePath, source);
		_activeIncludes.Add(filePath);
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
			if (_currentContext.Location.Line < _currentContext.Source.Length)
			{
				Location sourceLineLocation = _currentContext.Location;
				sourceLineLocation.Column = 0;
				_sourceLines.Add(new(sourceLineLocation, _currentContext.Source[_currentContext.Location.Line]));
			}

			// If end of an included file, pop the context from the stack
			if (_contextStack.Count > 0)
			{
				_activeIncludes.Remove(_currentContext.Location.FilePath);
				_currentContext = _contextStack.Pop();
			}
			// Otherwise, EOF
			else
			{
				SymbolToken token = MakeSymbolToken(Constants.TokenType.EndOfFile);
				result.ResultObject = token;
			}
		}
		else if (IsNewLine(c))
		{
			Location sourceLineLocation = _currentContext.Location;
			sourceLineLocation.Column = 0;
			_sourceLines.Add(new(sourceLineLocation, _currentContext.Source[_currentContext.Location.Line]));

			SymbolToken token = MakeSymbolToken(Constants.TokenType.NewLine);
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

			IdentifierToken it = identifierResult.ResultObject;

			// If this is an include directive, process that
			if (
				string.Equals(it.Lexeme, "include", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(it.Lexeme, ".include", StringComparison.OrdinalIgnoreCase)
			)
			{
				Result includeResult = TryProcessInclude(it.Location);
				result.Combine(includeResult);
			}
			// Otherwise it's a normal token
			else
			{
				result.ResultObject = identifierResult.ResultObject;
			}
		}
		else
		{
			result.AddError("Lexer", $"{_currentContext.Location} unexpected character {c}");
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
				token = MakeSymbolToken(Constants.TokenType.ShiftLeft);
				matchedTwo = true;
				break;
			case ('>', '>'):
				token = MakeSymbolToken(Constants.TokenType.ShiftRight);
				matchedTwo = true;
				break;
			case ('=', '='):
				token = MakeSymbolToken(Constants.TokenType.Equal);
				matchedTwo = true;
				break;
			case ('!', '='):
				token = MakeSymbolToken(Constants.TokenType.NotEqual);
				matchedTwo = true;
				break;
			case ('>', '='):
				token = MakeSymbolToken(Constants.TokenType.GreaterEqual);
				matchedTwo = true;
				break;
			case ('<', '='):
				token = MakeSymbolToken(Constants.TokenType.LessEqual);
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
				token = MakeSymbolToken(Constants.TokenType.Comma);
				matchedOne = true;
				break;
			case ':':
				token = MakeSymbolToken(Constants.TokenType.Colon);
				matchedOne = true;
				break;
			case '(':
				token = MakeSymbolToken(Constants.TokenType.LParen);
				matchedOne = true;
				break;
			case ')':
				token = MakeSymbolToken(Constants.TokenType.RParen);
				matchedOne = true;
				break;
			case '[':
				token = MakeSymbolToken(Constants.TokenType.LBracket);
				matchedOne = true;
				break;
			case ']':
				token = MakeSymbolToken(Constants.TokenType.RBracket);
				matchedOne = true;
				break;
			case '+':
				token = MakeSymbolToken(Constants.TokenType.Plus);
				matchedOne = true;
				break;
			case '-':
				token = MakeSymbolToken(Constants.TokenType.Minus);
				matchedOne = true;
				break;
			case '*':
				token = MakeSymbolToken(Constants.TokenType.Star);
				matchedOne = true;
				break;
			case '/':
				token = MakeSymbolToken(Constants.TokenType.Slash);
				matchedOne = true;
				break;
			case '%':
				token = MakeSymbolToken(Constants.TokenType.Percent);
				matchedOne = true;
				break;
			case '&':
				token = MakeSymbolToken(Constants.TokenType.Ampersand);
				matchedOne = true;
				break;
			case '|':
				token = MakeSymbolToken(Constants.TokenType.Pipe);
				matchedOne = true;
				break;
			case '^':
				token = MakeSymbolToken(Constants.TokenType.Caret);
				matchedOne = true;
				break;
			case '~':
				token = MakeSymbolToken(Constants.TokenType.Tilde);
				matchedOne = true;
				break;
			case '>':
				token = MakeSymbolToken(Constants.TokenType.Greater);
				matchedOne = true;
				break;
			case '<':
				token = MakeSymbolToken(Constants.TokenType.Less);
				matchedOne = true;
				break;
			case '!':
				token = MakeSymbolToken(Constants.TokenType.Exclamation);
				matchedOne = true;
				break;
			case '$':
				token = MakeSymbolToken(Constants.TokenType.Dollar);
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

		int startColumn = _currentContext.Location.Column;

		Advance();
		c = Current();

		if (c == '\'')
		{
			result.AddError("Lexer", $"{_currentContext.Location} expected character in character literal");
			result.ResultObject = MakeNumberToken("''", 0);
			Advance();
		}
		else
		{
			Result<long> parseResult = ParseCharInternal();
			result.Combine(parseResult);

			c = Current();

			if (c != '\'')
			{
				result.AddError("Lexer", $"{_currentContext.Location} expected end of character literal");
			}
			else
			{
				Advance();
			}

			result.ResultObject = MakeNumberToken(
				_currentContext.Source[_currentContext.Location.Line][startColumn.._currentContext.Location.Column],
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

		Location startLocation = _currentContext.Location;

		Advance();
		c = Current();

		while (c != Constants.StringDelim)
		{
			if (IsNewLine(c) || c == '\0')
			{
				result.AddError("Lexer", $"{_currentContext.Location} expected end of string literal");
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

		result.ResultObject = MakeStringToken(
			startLocation,
			_currentContext.Source[_currentContext.Location.Line][startLocation.Column.._currentContext.Location.Column],
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

		Location startLocation = _currentContext.Location;

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
			result.AddError("Lexer", $"{startLocation}: number literal value too large \"{lexeme}\"");
		}
		catch (Exception)
		{
			result.AddError("Lexer", $"{startLocation} invalid number literal \"{lexeme}\"");
		}


		result.ResultObject = MakeNumberToken(startLocation, lexeme, value);

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

		int startColumn = _currentContext.Location.Column;
		Location startLocation = _currentContext.Location;

		while (IsIdentifierChar(c))
		{
			Advance();
			c = Current();
		}

		result.ResultObject = MakeIdentifierToken(
			startLocation,
			_currentContext.Source[_currentContext.Location.Line][startColumn.._currentContext.Location.Column]
		);

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

		Location startLocation = _currentContext.Location;

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
							$"{startLocation} expected two hex characters for escaped hex value"
						);
						break;
					}

					Advance();
					char lower = Current();
					if (!char.IsAsciiHexDigit(lower))
					{
						result.AddError(
							"Lexer",
							$"{startLocation} expected two hex characters for escaped hex value"
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
						result.AddError("Lexer", $"{startLocation} invalid escaped hex value \"\\x{full}\"");
					}

					Advance();

					break;
				}
				default:
				{
					result.AddError("Lexer", $"{startLocation} invalid escape sequence \"\\{c}\"");
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

	private Result TryProcessInclude(Location includeLocation)
	{
		Result result = new();

		// Expect a string literal to follow include
		SkipWhiteSpace();

		if (!TryParseStringLiteral(out Result<StringToken?> pathResult))
		{
			result.AddError("Lexer", $"{includeLocation} expected string literal after \"include\"");
			return result;
		}

		Debug.Assert(pathResult.ResultObject is not null);

		result.Combine(pathResult);
		if (!pathResult.IsSuccess)
		{
			return result;
		}

		string rawPath = Encoding.UTF8.GetString([.. pathResult.ResultObject.StringData]);

		// Resolve relative to the directory of the file doing the including, not the CWD.
		string baseDir = Path.GetDirectoryName(_currentContext.Location.FilePath) ?? string.Empty;
		string resolvedPath = Path.GetFullPath(Path.Combine(baseDir, rawPath));

		if (!File.Exists(resolvedPath))
		{
			result.AddError("Lexer", $"{includeLocation} cannot find include file \"{rawPath}\"");
			return result;
		}

		if (_activeIncludes.Contains(resolvedPath))
		{
			result.AddError("Lexer", $"{includeLocation} circular include of \"{rawPath}\"");
			return result;
		}

		string[] includedSource;
		try
		{
			includedSource = File.ReadAllLines(resolvedPath);
		}
		catch (Exception ex)
		{
			result.AddError("Lexer", $"{includeLocation} failed to read include file \"{rawPath}\": {ex.Message}");
			return result;
		}

		// Save current context, overwrite it with new context
		_contextStack.Push(_currentContext);
		_activeIncludes.Add(resolvedPath);
		_currentContext = new(
			resolvedPath,
			includedSource
		);

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
		_currentContext.Location.Line++;
		_currentContext.Location.Column = 0;
	}

	private char Current()
	{
		if (_currentContext.Location.Line >= _currentContext.Source.Length)
		{
			return '\0';
		}

		if (_currentContext.Location.Column >= _currentContext.Source[_currentContext.Location.Line].Length)
		{
			return '\n';
		}

		return _currentContext.Source[_currentContext.Location.Line][_currentContext.Location.Column];
	}

	private char Next()
	{
		if (_currentContext.Location.Line >= _currentContext.Source.Length)
		{
			return '\0';
		}

		if (_currentContext.Location.Column + 1 >= _currentContext.Source[_currentContext.Location.Line].Length)
		{
			return '\n';
		}

		return _currentContext.Source[_currentContext.Location.Line][_currentContext.Location.Column + 1];
	}

	private void Advance(int count = 1)
	{
		_currentContext.Location.Column += count;
	}

	private SymbolToken MakeSymbolToken(Constants.TokenType type)
	{
		return new(_currentContext.Location, type);
	}

	private IdentifierToken MakeIdentifierToken(Location location, string lexeme)
	{
		return new(location, lexeme);
	}

	private NumberToken MakeNumberToken(string lexeme, long value)
	{
		return new(_currentContext.Location, lexeme, value);
	}

	private NumberToken MakeNumberToken(Location location, string lexeme, long value)
	{
		return new(location, lexeme, value);
	}

	private StringToken MakeStringToken(Location location, string lexeme, List<byte> stringData)
	{
		return new(location, lexeme, stringData);
	}
}
