namespace IM800Asm;

internal class ExpressionEvaluator
{
	private List<Token> _tokens;
	private Func<string, long?> _resolveSymbol;
	private int _locationCounter;
	private int _position;

	public ExpressionEvaluator(List<Token> tokens, Func<string, long?> resolveSymbol, int locationCounter)
	{
		_tokens = tokens;
		_resolveSymbol = resolveSymbol;
		_locationCounter = locationCounter;
		_position = 0;
	}

	public Result<long> Evaluate()
	{
		Result<long> result = new(0);

		long value = ParseComparison(result);
		result.ResultObject = value;

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
					}
					else if (it.Lexeme.Equals(Constants.ShiftRightAlias, StringComparison.OrdinalIgnoreCase))
					{
						type = Constants.TokenType.ShiftRight;
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
				result.AddError("Expression", $"{p.Line}:{p.Column}:\tdivision by zero");
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
				result.AddError("Expression", $"{it.Line}:{it.Column}:\tundefined symbol \"{it.Lexeme}\"");
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
					result.AddError("Expression", $"{t.Line}:{t.Column}\texpected ')'");
				}

				return value;
			}
		}

		result.AddError("Expression", $"{t.Line}:{t.Column}:\texpected value, got {t.ToShortString()}");
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
			return new SymbolToken(0, 0, Constants.TokenType.EndOfFile);
		}

		return _tokens[_position];
	}

	private void Advance(int count = 1)
	{
		_position += count;
	}
}