using System.Collections;

namespace IM800Asm;

internal class ExpressionEvaluator
{
	private List<Token> _tokens;
	private Func<string, long?> _resolveSymbol;
	private long _locationCounter;
	private int _position;

	public ExpressionEvaluator(Func<string, long?> resolveSymbol)
	{
		_tokens = [];
		_resolveSymbol = resolveSymbol;
		_locationCounter = 0;
		_position = 0;
	}

	public Result<long> Evaluate(List<Token> tokens, long locationCounter, Constants.Size size, Constants.Signedness signed)
	{
		Result<long> result = new(0);

		if (tokens.Count == 0)
		{
			throw new Exception("expected expression tokens count to be validated before evaluation");
		}

		_tokens = tokens;
		_locationCounter = locationCounter;
		_position = 0;

		long value = ParseComparison(result);
		result.ResultObject = value;

		Token firstToken = tokens[0];
		Result<long> rangeResult = ValidateTruncateRange(firstToken.Location, value, size, signed);
		result.Combine(rangeResult);

		return result;
	}

	private static Result<long> ValidateTruncateRange(
		Location location,
		long value,
		Constants.Size size,
		Constants.Signedness signed
	)
	{
		Result<long> result = new(0);

		long min;
		long max;
		long truncated;

		switch ((size, signed))
		{
			case (Constants.Size.Byte, Constants.Signedness.Either):
			{
				min = sbyte.MinValue;
				max = byte.MaxValue;
				truncated = (byte)value; // sign bits are still kept and truncated for assembled program
				break;
			}
			case (Constants.Size.Byte, Constants.Signedness.Signed):
			{
				min = sbyte.MinValue;
				max = sbyte.MaxValue;
				truncated = (byte)value;
				break;
			}
			case (Constants.Size.Byte, Constants.Signedness.Unsigned):
			{
				min = byte.MinValue;
				max = byte.MaxValue;
				truncated = (byte)value;
				break;
			}
			case (Constants.Size.Word, Constants.Signedness.Either):
			{
				min = short.MinValue;
				max = ushort.MaxValue;
				truncated = (ushort)value;
				break;
			}
			case (Constants.Size.Word, Constants.Signedness.Signed):
			{
				min = short.MinValue;
				max = short.MaxValue;
				truncated = (ushort)value;
				break;
			}
			case (Constants.Size.Word, Constants.Signedness.Unsigned):
			{
				min = ushort.MinValue;
				max = ushort.MaxValue;
				truncated = (ushort)value;
				break;
			}
			case (Constants.Size.Dword, Constants.Signedness.Either):
			{
				min = int.MinValue;
				max = uint.MaxValue;
				truncated = (uint)value;
				break;
			}
			case (Constants.Size.Dword, Constants.Signedness.Signed):
			{
				min = int.MinValue;
				max = int.MaxValue;
				truncated = (uint)value;
				break;
			}
			case (Constants.Size.Dword, Constants.Signedness.Unsigned):
			{
				min = uint.MinValue;
				max = uint.MaxValue;
				truncated = (uint)value;
				break;
			}
			default:
			{
				min = long.MinValue;
				max = long.MaxValue;
				truncated = value;
				break;
			}
		}

		if (value < min || value > max)
		{
			string signedDisplay = signed switch
			{
				Constants.Signedness.Either => string.Empty,
				Constants.Signedness.Signed => "Signed ",
				Constants.Signedness.Unsigned => "Unsigned ",
				_ => throw new Exception($"unknown signedness {signed}"),
			};

			result.AddWarning(
				"Expression",
				$"{location} value {value} truncated to {signedDisplay}{size} {truncated}"
			);
		}

		result.ResultObject = truncated;
		return result;
	}

	// takes in a result parameter because otherwise it's a lot of annoying result.Combine()
	// and there are like three cases where we add errors way down the chain.
	private long ParseComparison(Result result)
	{
		long left = ParseOr(result);

		Constants.TokenType[] acceptedTypes = [
			Constants.TokenType.Equal,
			Constants.TokenType.NotEqual,
			Constants.TokenType.Greater,
			Constants.TokenType.GreaterEqual,
			Constants.TokenType.Less,
			Constants.TokenType.LessEqual,
		];

		while (TryMatchSymbol(out Constants.TokenType type, acceptedTypes))
		{
			long right = ParseOr(result);

			left = type switch
			{
				Constants.TokenType.Equal => left == right ? 1 : 0,
				Constants.TokenType.NotEqual => left != right ? 1 : 0,
				Constants.TokenType.Greater => left > right ? 1 : 0,
				Constants.TokenType.GreaterEqual => left >= right ? 1 : 0,
				Constants.TokenType.Less => left < right ? 1 : 0,
				Constants.TokenType.LessEqual => left <= right ? 1 : 0,
				_ => throw new Exception($"Unexpected comparison type {type}"),
			};
		}

		return left;
	}

	private long ParseOr(Result result)
	{
		long left = ParseXor(result);

		while (TryMatchSymbol(out Constants.TokenType _, Constants.TokenType.Pipe))
		{
			long right = ParseXor(result);

			left |= right;
		}

		return left;
	}

	private long ParseXor(Result result)
	{
		long left = ParseAnd(result);

		while (TryMatchSymbol(out Constants.TokenType _, Constants.TokenType.Caret))
		{
			long right = ParseAnd(result);

			left ^= right;
		}

		return left;
	}

	private long ParseAnd(Result result)
	{
		long left = ParseShift(result);

		while (TryMatchSymbol(out Constants.TokenType _, Constants.TokenType.Ampersand))
		{
			long right = ParseShift(result);

			left &= right;
		}

		return left;
	}

	private long ParseShift(Result result)
	{
		long left = ParseAddSub(result);

		Constants.TokenType[] acceptedTypes = [
				Constants.TokenType.ShiftLeft,
				Constants.TokenType.ShiftRight,
			];

		while (true)
		{
			Constants.TokenType type = default;

			TryMatchSymbol(out type, acceptedTypes);

			// try aliases
			if (type == default)
			{
				Token c = Current();

				if (c is IdentifierToken it)
				{
					if (it.Lexeme.Equals(Constants.ShiftLeftAlias, StringComparison.OrdinalIgnoreCase))
					{
						type = Constants.TokenType.ShiftLeft;
						Advance();
					}
					else if (it.Lexeme.Equals(Constants.ShiftRightAlias, StringComparison.OrdinalIgnoreCase))
					{
						type = Constants.TokenType.ShiftRight;
						Advance();
					}
				}
			}

			if (type == default)
			{
				break;
			}

			long right = ParseAddSub(result);

			left = type switch
			{
				Constants.TokenType.ShiftLeft => left << (int)right,
				Constants.TokenType.ShiftRight => left >> (int)right,
				_ => throw new Exception($"Unexpected shift type {type}"),
			};
		}

		return left;
	}

	private long ParseAddSub(Result result)
	{
		long left = ParseMultDiv(result);

		Constants.TokenType[] acceptedTypes = [
			Constants.TokenType.Plus,
			Constants.TokenType.Minus,
		];

		while (TryMatchSymbol(out Constants.TokenType type, acceptedTypes))
		{
			long right = ParseMultDiv(result);

			left = type switch
			{
				Constants.TokenType.Plus => left + right,
				Constants.TokenType.Minus => left - right,
				_ => throw new Exception($"Unexpected add/sub type {type}"),
			};
		}

		return left;
	}

	private long ParseMultDiv(Result result)
	{
		long left = ParseUnary(result);

		Constants.TokenType[] acceptedTypes = [
				Constants.TokenType.Star,
				Constants.TokenType.Slash,
				Constants.TokenType.Percent,
			];

		while (true)
		{
			Constants.TokenType type = default;

			TryMatchSymbol(out type, acceptedTypes);

			// try alias
			if (type == default)
			{
				Token c = Current();

				if (c is IdentifierToken it)
				{
					if (it.Lexeme.Equals(Constants.ModuloAlias, StringComparison.OrdinalIgnoreCase))
					{
						type = Constants.TokenType.Percent;
						Advance();
					}
				}
			}

			if (type == default)
			{
				break;
			}

			long right = ParseUnary(result);

			if (type is Constants.TokenType.Slash or Constants.TokenType.Percent && right == 0)
			{
				// Get previous token (last token of add/sub)
				Token p = _tokens[_position - 1];
				result.AddError("Expression", $"{p.Location} division by zero");
				left = 0;
			}
			else
			{
				left = type switch
				{
					Constants.TokenType.Star => left * right,
					Constants.TokenType.Slash => left / right,
					Constants.TokenType.Percent => left % right,
					_ => throw new Exception($"Unexpected mul/div type {type}"),
				};
			}
		}

		return left;
	}

	private long ParseUnary(Result result)
	{
		Constants.TokenType[] acceptedTypes = [
			Constants.TokenType.Tilde,
			Constants.TokenType.Minus,
			Constants.TokenType.Plus,
			Constants.TokenType.Exclamation,
		];

		if (TryMatchSymbol(out Constants.TokenType type, acceptedTypes))
		{
			long operand = ParseUnary(result);

			return type switch
			{
				Constants.TokenType.Tilde => ~operand,
				Constants.TokenType.Minus => -operand,
				Constants.TokenType.Plus => operand,
				Constants.TokenType.Exclamation => operand == 0 ? 1 : 0,
				_ => throw new Exception($"Unexpected unary type {type}"),
			};
		}

		return ParsePrimary(result);
	}

	private long ParsePrimary(Result result)
	{
		Token t = Current();
		Advance();

		if (t is NumberToken nt)
		{
			return nt.Value;
		}

		if (t is IdentifierToken it)
		{
			long? value = _resolveSymbol(it.Lexeme);

			if (value is null)
			{
				result.AddError("Expression", $"{it.Location} undefined symbol \"{it.Lexeme}\"");
				return 0;
			}

			return value.Value;
		}

		if (t is SymbolToken st)
		{
			if (st.Type == Constants.TokenType.Dollar)
			{
				return _locationCounter;
			}

			if (st.Type == Constants.TokenType.LParen)
			{
				long value = ParseComparison(result);

				t = Current();

				if (t is SymbolToken rp && rp.Type == Constants.TokenType.RParen)
				{
					Advance();
				}
				else
				{
					result.AddError("Expression", $"{t.Location} expected ')'");
				}

				return value;
			}
		}

		result.AddError("Expression", $"{t.Location} expected value, got {t.ToShortString()}");
		return 0;
	}

	private bool TryMatchSymbol(out Constants.TokenType matchedType, params Constants.TokenType[] types)
	{
		matchedType = default;

		Token c = Current();

		if (c is SymbolToken st && types.IndexOf(st.Type) >= 0)
		{
			matchedType = st.Type;
			Advance();
			return true;
		}

		return false;
	}

	private Token Current()
	{
		if (_position >= _tokens.Count)
		{
			Location location = new(string.Empty, 0, 0);
			return new SymbolToken(location, Constants.TokenType.EndOfFile);
		}

		return _tokens[_position];
	}

	private void Advance(int count = 1)
	{
		_position += count;
	}
}