using System.Diagnostics;
using IM800Asm.Core;
using IM800Asm.Lexing;

namespace IM800Asm.Parsing;

internal class Parser(List<Token> tokens)
{
	private readonly List<Statement> _statements = [];
	private int _position;

	public Result<List<Statement>> Parse()
	{
		Result<List<Statement>> result = new(_statements);

		while (true)
		{
			Result<Statement?> statementResult = NextStatement();
			result.Combine(statementResult);

			if (statementResult.ResultObject is null)
			{
				continue;
			}

			_statements.Add(statementResult.ResultObject);

			if (statementResult.ResultObject is EndOfFileStatement)
			{
				break;
			}
		}

		return result;
	}

	public Result<Statement?> NextStatement()
	{
		Result<Statement?> result = new(null);

		Token token = Current();

		if (token is SymbolToken { Type: Constants.TokenType.EndOfFile } t)
		{
			result.ResultObject = new EndOfFileStatement(t.SourceLocation);
		}
		else if (token is SymbolToken { Type: Constants.TokenType.NewLine })
		{
			Advance();
		}
		else if (TryParseLabel(out Result<LabelStatement?> labelResult))
		{
			Debug.Assert(labelResult.ResultObject is not null);
			result.Combine(labelResult);
			result.ResultObject = labelResult.ResultObject;
		}
		else if (TryParseDirective(out Result<DirectiveStatement?> directiveResult))
		{
			Debug.Assert(directiveResult.ResultObject is not null);
			result.Combine(directiveResult);
			result.ResultObject = directiveResult.ResultObject;
		}
		else if (TryParseInstruction(out Result<InstructionStatement?> instructionResult))
		{
			Debug.Assert(instructionResult.ResultObject is not null);
			result.Combine(instructionResult);
			result.ResultObject = instructionResult.ResultObject;
		}
		else
		{
			result.AddError(
				token.SourceLocation,
				Constants.ErrorCode.UnexpectedToken,
				$"unexpected token {token.ToShortString()}"
			);
			Advance();
		}

		return result;
	}

	private bool TryParseLabel(out Result<LabelStatement?> result)
	{
		result = new Result<LabelStatement?>(null);

		if (Current() is IdentifierToken t && Next() is SymbolToken { Type: Constants.TokenType.Colon })
		{
			result.ResultObject = new LabelStatement(t.SourceLocation, t.Lexeme);
			Advance(2);
			return true;
		}

		return false;
	}

	private bool TryParseInstruction(out Result<InstructionStatement?> result)
	{
		result = new Result<InstructionStatement?>(null);

		if (Current() is IdentifierToken t)
		{
			string canon = t.Lexeme.ToUpperInvariant();
			Constants.Size? size = null;

			if (canon.Length < 2)
			{
				return false;
			}

			// A size suffix is exactly one character after the separator, e.g. "LD.B".
			if (canon[^2] == '.' && TryParseInstructionSize(canon[^1], out Constants.Size? parsedSize))
			{
				size = parsedSize;
				canon = canon[..^2];
			}

			if (Enum.TryParse(canon, true, out Constants.Instruction instruction))
			{
				InstructionStatement statement = new(t.SourceLocation, instruction, size);
				Advance();

				Result<List<Operand>> operandsResult = ParseOperands();
				result.Combine(operandsResult);
				statement.Operands = operandsResult.ResultObject;

				result.ResultObject = statement;
				return true;
			}
		}

		return false;
	}

	private static bool TryParseInstructionSize(char suffix, out Constants.Size? size)
	{
		switch (char.ToUpper(suffix))
		{
			case 'B':
				size = Constants.Size.Byte;
				return true;
			case 'W':
				size = Constants.Size.Word;
				return true;
			case 'D':
				size = Constants.Size.Dword;
				return true;
			default:
				size = null;
				return false;
		}
	}

	private bool TryParseDirective(out Result<DirectiveStatement?> result)
	{
		result = new Result<DirectiveStatement?>(null);

		if (Current() is IdentifierToken t)
		{
			string mnemonic = t.Lexeme;

			// Directives may have a leading period, e.g. ".ORG".
			if (mnemonic.StartsWith('.'))
			{
				mnemonic = mnemonic[1..];
			}

			if (Enum.TryParse(mnemonic, true, out Constants.Directive directive))
			{
				DirectiveStatement statement = new(t.SourceLocation, directive);
				Advance();

				Result<List<Operand>> operandsResult = ParseOperands();
				result.Combine(operandsResult);
				statement.Operands = operandsResult.ResultObject;

				result.ResultObject = statement;
				return true;
			}
		}

		return false;
	}

	private Result<List<Operand>> ParseOperands()
	{
		List<Operand> operands = [];
		Result<List<Operand>> result = new(operands);

		while (true)
		{
			Token c = Current();

			if (IsEndOfOperandList(c))
			{
				break;
			}

			if (c is SymbolToken { Type: Constants.TokenType.LBracket })
			{
				Result<Operand?> memoryOperandResult = ParseMemoryOperand();
				result.Combine(memoryOperandResult);

				if (memoryOperandResult.ResultObject is not null)
				{
					operands.Add(memoryOperandResult.ResultObject);
				}
			}
			else if (c is StringToken st)
			{
				// A string decomposes into a list of byte literals
				operands.AddRange(ParseStringOperand(st));
				Advance();
			}
			else
			{
				Result<Operand?> operandResult = ParseOperand();
				result.Combine(operandResult);

				if (operandResult.ResultObject is not null)
				{
					operands.Add(operandResult.ResultObject);
				}
			}

			c = Current();

			if (c is SymbolToken { Type: Constants.TokenType.Comma })
			{
				// Allow ending a line on a comma to continue list on new line
				if (Next() is SymbolToken { Type: Constants.TokenType.NewLine })
				{
					Advance(2);
				}
				else
				{
					Advance();
				}
			}
			else if (!IsEndOfOperand(c))
			{
				result.AddError(c.SourceLocation, Constants.ErrorCode.UnexpectedOperand, "expected end of operand");
				Advance();
			}
		}

		return result;
	}

	private static List<Operand> ParseStringOperand(StringToken token)
	{
		List<Operand> operands = [];

		foreach (byte b in token.StringData)
		{
			NumberToken byteToken = new(token.SourceLocation, b.ToString(), b);
			operands.Add(new ExpressionOperand(token.SourceLocation, [byteToken]));
		}

		return operands;
	}

	private Result<Operand?> ParseOperand()
	{
		Result<Operand?> result = new(null);

		Token c = Current();

		if (TryParseRegisterOperand(out Result<RegisterOperand?> registerResult))
		{
			Debug.Assert(registerResult.ResultObject is not null);
			result.Combine(registerResult);
			result.ResultObject = registerResult.ResultObject;
		}
		else if (TryParseConditionOperand(out Result<ConditionOperand?> conditionResult))
		{
			Debug.Assert(conditionResult.ResultObject is not null);
			result.Combine(conditionResult);
			result.ResultObject = conditionResult.ResultObject;
		}
		else if (TryParseBlockOperand(out Result<BlockOperand?> blockResult))
		{
			Debug.Assert(blockResult.ResultObject is not null);
			result.Combine(blockResult);
			result.ResultObject = blockResult.ResultObject;
		}
		else if (TryParseSizeOperand(out Result<SizeOperand?> sizeResult))
		{
			Debug.Assert(sizeResult.ResultObject is not null);
			result.Combine(sizeResult);
			result.ResultObject = sizeResult.ResultObject;
		}
		else if (TryParseExpressionOperand(out Result<ExpressionOperand?> expressionResult))
		{
			Debug.Assert(expressionResult.ResultObject is not null);
			result.Combine(expressionResult);
			result.ResultObject = expressionResult.ResultObject;
		}
		else
		{
			result.AddError(c.SourceLocation, Constants.ErrorCode.UnexpectedToken, $"unexpected token {c}");
			Advance();
		}

		return result;
	}

	private Result<Operand?> ParseMemoryOperand()
	{
		Result<Operand?> result = new(null);

		Token start = Current(); // '['
		Advance();

		Token inner = Current();

		if (inner is IdentifierToken idt && Enum.TryParse(idt.Lexeme, true, out Constants.Register register))
		{
			Advance();

			if (Current() is SymbolToken { Type: Constants.TokenType.Plus or Constants.TokenType.Minus })
			{
				// indexed
				List<Token> expressionTokens = ParseExpression();

				result.ResultObject = new IndexedOperand(start.SourceLocation, register, expressionTokens);
			}
			else
			{
				// indirect
				result.ResultObject = new IndirectRegisterOperand(start.SourceLocation, register);
			}
		}
		else if (IsExpressionStart(inner))
		{
			// direct
			List<Token> expressionTokens = ParseExpression();
			result.ResultObject = new IndirectExpressionOperand(start.SourceLocation, expressionTokens);
		}
		else
		{
			result.AddError(
				inner.SourceLocation,
				Constants.ErrorCode.ExpectedOperand,
				"expected register or expression"
			);
			Advance();
		}

		if (Current() is SymbolToken { Type: Constants.TokenType.RBracket })
		{
			Advance();
		}
		else
		{
			Token c = Current();
			result.AddError(c.SourceLocation, Constants.ErrorCode.UnterminatedIndirectOperand, "expected ']'");
		}

		return result;
	}

	private bool TryParseRegisterOperand(out Result<RegisterOperand?> result)
	{
		result = new Result<RegisterOperand?>(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, true, out Constants.Register register)
		)
		{
			result.ResultObject = new RegisterOperand(t.SourceLocation, register);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseConditionOperand(out Result<ConditionOperand?> result)
	{
		result = new Result<ConditionOperand?>(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, true, out Constants.Condition condition)
		)
		{
			result.ResultObject = new ConditionOperand(t.SourceLocation, condition);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseBlockOperand(out Result<BlockOperand?> result)
	{
		result = new Result<BlockOperand?>(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, true, out Constants.Block block)
		)
		{
			result.ResultObject = new BlockOperand(t.SourceLocation, block);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseSizeOperand(out Result<SizeOperand?> result)
	{
		result = new Result<SizeOperand?>(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, true, out Constants.Size size)
		)
		{
			// Internal enumeration used by the instruction table
			if (size == Constants.Size.Unsized)
			{
				return false;
			}

			result.ResultObject = new SizeOperand(t.SourceLocation, size);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseExpressionOperand(out Result<ExpressionOperand?> result)
	{
		result = new Result<ExpressionOperand?>(null);

		if (!IsExpressionStart(Current()))
		{
			return false;
		}

		Token start = Current();
		List<Token> expressionTokens = ParseExpression();
		result.ResultObject = new ExpressionOperand(start.SourceLocation, expressionTokens);

		return true;
	}

	private List<Token> ParseExpression()
	{
		// Gather all tokens in this operand, recursive descent parser will be later
		List<Token> expressionTokens = [];
		Token t = Current();

		while (!IsEndOfExpression(t))
		{
			expressionTokens.Add(t);
			Advance();
			t = Current();
		}

		return expressionTokens;
	}

	private static bool IsExpressionStart(Token token)
	{
		if (token is NumberToken or IdentifierToken)
		{
			return true;
		}

		return token is SymbolToken
		{
			Type: Constants.TokenType.Plus
			or Constants.TokenType.Minus
			or Constants.TokenType.Tilde
			or Constants.TokenType.Exclamation
			or Constants.TokenType.Dollar
			or Constants.TokenType.LParen
		};
	}

	private static bool IsEndOfExpression(Token token)
	{
		return token is SymbolToken
		{
			Type: Constants.TokenType.Comma
			or Constants.TokenType.NewLine
			or Constants.TokenType.EndOfFile
			or Constants.TokenType.RBracket
		};
	}

	private static bool IsEndOfOperand(Token token)
	{
		return token is SymbolToken
		{
			Type: Constants.TokenType.Comma
			or Constants.TokenType.NewLine
			or Constants.TokenType.EndOfFile
		};
	}

	private static bool IsEndOfOperandList(Token token)
	{
		return token is SymbolToken
		{
			Type: Constants.TokenType.NewLine
			or Constants.TokenType.EndOfFile
		};
	}

	private Token Current()
	{
		if (_position >= tokens.Count)
		{
			SourceLocation sourceLocation = new(string.Empty, 0, 0);
			return new SymbolToken(sourceLocation, Constants.TokenType.EndOfFile);
		}

		return tokens[_position];
	}

	private Token Next()
	{
		if (_position + 1 >= tokens.Count)
		{
			SourceLocation sourceLocation = new(string.Empty, 0, 0);
			return new SymbolToken(sourceLocation, Constants.TokenType.EndOfFile);
		}

		return tokens[_position + 1];
	}

	private void Advance(int count = 1)
	{
		_position += count;
	}
}
