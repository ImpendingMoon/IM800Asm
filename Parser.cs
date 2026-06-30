using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;

namespace IM800Asm;

internal class Parser
{
	private List<Token> _tokens;
	private List<Statement> _statements;
	private int _position;

	public Parser(List<Token> tokens)
	{
		_tokens = tokens;
		_statements = [];
		_position = 0;
	}

	public Result<List<Statement>> Parse()
	{
		Result<List<Statement>> result = new(_statements);

		while (true)
		{
			Result<Statement?> statementResult = NextStatement();
			result.Combine(statementResult);

			if (statementResult.ResultObject is not null)
			{
				_statements.Add(statementResult.ResultObject);

				if (statementResult.ResultObject is EndOfFileStatement)
				{
					break;
				}
			}
		}

		return result;
	}

	public Result<Statement?> NextStatement()
	{
		Result<Statement?> result = new(null);

		Token token = Current();

		if (token is SymbolToken t && t.Type == Constants.TokenType.EndOfFile)
		{
			result.ResultObject = new EndOfFileStatement(t.Line, t.Column);
		}
		else if (TryParseLabel(out Result<LabelStatement?> labelResult))
		{
			Debug.Assert(labelResult.ResultObject is not null);
			result.Combine(labelResult);
			result.ResultObject = labelResult.ResultObject;
		}
		else
		{
			result.AddError("Parser", $"{token.Line}:{token.Column}:\tunexpected token {token}");
			Advance();
		}

		return result;
	}

	private bool TryParseLabel(out Result<LabelStatement?> result)
	{
		result = new(null);

		if (Current() is IdentifierToken t && Next() is SymbolToken n && n.Type == Constants.TokenType.Colon)
		{
			result.ResultObject = new(t.Line, t.Column, t.Lexeme);
			Advance(2);
		}

		return false;
	}

	private bool TryParseInstruction(out Result<InstructionStatement?> result)
	{
		result = new(null);

		if (Current() is IdentifierToken t)
		{
			// TODO: strip size suffix (ends with /\../ )
			// TODO: tryparse instruction
			// TODO: parseoperands
		}

		return false;
	}

	private bool TryParseInstructionSize(char suffix, out Constants.Size? size)
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
		result = new(null);

		if (Current() is IdentifierToken t)
		{
			// TODO strip optional leading period
			// TODO tryparse directive
			// TODO parseoperands
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

			if (c is SymbolToken t && t.Type == Constants.TokenType.LBracket)
			{
				Result<Operand?> memoryOperandResult = ParseMemoryOperand();
				result.Combine(memoryOperandResult);

				if (memoryOperandResult.ResultObject is not null)
				{
					operands.Add(memoryOperandResult.ResultObject);
				}
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

			if (IsEndOfOperand(c))
			{
				// Allow ending a line on a comma to continue list on new line
				if (
					c is SymbolToken ct && ct.Type == Constants.TokenType.Comma
					&& Next() is SymbolToken n && n.Type == Constants.TokenType.NewLine
				)
				{
					Advance(2);
				}
				else
				{
					Advance();
				}
			}
			else
			{
				result.AddError("Parser", $"{c.Line}:{c.Column}:\texpected end of operand");
			}
		}

		return result;
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
			result.AddError("Parser", $"{c.Line}:{c.Column}:\tunexpected token {c}");
			Advance();
		}

		return result;
	}

	private Result<Operand?> ParseMemoryOperand()
	{
		Result<Operand?> result = new(null);

		Token c = Current();

		// TODO: Handle indirect register/expression, indexed

		// TODO: Expect and consume RBracket before end of operand

		return result;
	}

	private bool TryParseRegisterOperand(out Result<RegisterOperand?> result)
	{
		result = new(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, ignoreCase: true, out Constants.Register register)
		)
		{
			result.ResultObject = new RegisterOperand(t.Line, t.Column, register);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseConditionOperand(out Result<ConditionOperand?> result)
	{
		result = new(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, ignoreCase: true, out Constants.Condition condition)
		)
		{
			result.ResultObject = new ConditionOperand(t.Line, t.Column, condition);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseBlockOperand(out Result<BlockOperand?> result)
	{
		result = new(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, ignoreCase: true, out Constants.Block block)
		)
		{
			result.ResultObject = new BlockOperand(t.Line, t.Column, block);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseSizeOperand(out Result<SizeOperand?> result)
	{
		result = new(null);

		if (
			Current() is IdentifierToken t
			&& Enum.TryParse(t.Lexeme, ignoreCase: true, out Constants.Size size)
		)
		{
			result.ResultObject = new SizeOperand(t.Line, t.Column, size);
			Advance();
			return true;
		}

		return false;
	}

	private bool TryParseExpressionOperand(out Result<ExpressionOperand?> result)
	{
		result = new(null);

		if (Current() is IdentifierToken t)
		{
			// TODO recursive descent parser in new ExpresisonParser class
		}

		return false;
	}

	private static bool IsEndOfOperand(Token token)
	{
		return token is SymbolToken t
			&& t.Type
			is Constants.TokenType.Comma
			or Constants.TokenType.NewLine
			or Constants.TokenType.EndOfFile;
	}

	private static bool IsEndOfOperandList(Token token)
	{
		return token is SymbolToken t
			&& t.Type
			is Constants.TokenType.NewLine
			or Constants.TokenType.EndOfFile;
	}

	private Token Current()
	{
		if (_position >= _tokens.Count)
		{
			return new SymbolToken(0, 0, Constants.TokenType.EndOfFile);
		}

		return _tokens[_position];
	}

	private Token Next()
	{
		if (_position + 1 >= _tokens.Count)
		{
			return new SymbolToken(0, 0, Constants.TokenType.EndOfFile);
		}

		return _tokens[_position + 1];
	}

	private void Advance(int count = 1)
	{
		_position += count;
	}
}