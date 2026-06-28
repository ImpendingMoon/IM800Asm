using System.Diagnostics;

namespace IM800Asm
{
	internal class GrammarParser
	{
		private List<Token> _tokens;
		private List<Statement> _statements;
		private int _position;

		public GrammarParser(List<Token> tokens)
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

					if (statementResult.ResultObject.Type == Constants.StatementType.EndOfFile)
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

			Token t = Current();

			if (t.Type == Constants.TokenType.EndOfFile)
			{
				Statement statement = new(t.Line, t.Column, Constants.StatementType.EndOfFile);
				result.ResultObject = statement;
			}
			else if (t.Type == Constants.TokenType.NewLine)
			{
				Advance();
			}
			else if (TryParseLabelDefinition(out Result<Statement?> labelResult))
			{
				Debug.Assert(labelResult.ResultObject is not null);
				result.Combine(labelResult);
				result.ResultObject = labelResult.ResultObject;
			}
			else if (TryParseInstruction(out Result<Statement?> instructionResult))
			{
				Debug.Assert(instructionResult.ResultObject is not null);
				result.Combine(instructionResult);
				result.ResultObject = instructionResult.ResultObject;
			}
			else if (TryParseDirective(out Result<Statement?> directiveResult))
			{
				Debug.Assert(directiveResult.ResultObject is not null);
				result.Combine(directiveResult);
				result.ResultObject = directiveResult.ResultObject;
			}
			else
			{
				result.AddError("Parser", $"{t.Line}:{t.Column}:\tunexpected token {t}");
				Advance();
			}

			return result;
		}

		private bool TryParseLabelDefinition(out Result<Statement?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			// Ending colon denotes label definition
			if (!t.Lexeme.EndsWith(':'))
			{
				return false;
			}

			// Remove colon from the end
			string symbol = t.Lexeme[..^1];

			// Make sure it doesn't have another colon
			if (symbol.Contains(':'))
			{
				result.AddError("Parser", $"{t.Line}:{t.Column}:\tunexpected character \':\' in label");
			}

			Advance();
			result.ResultObject = new(t.Line, t.Column, Constants.StatementType.LabelDeclaration, t.Lexeme, symbol);
			return true;
		}

		private bool TryParseInstruction(out Result<Statement?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			if (t.Lexeme.Length < 2)
			{
				return false;
			}

			string canon = t.Lexeme.ToUpperInvariant();
			Constants.Size? size = null;

			// Parse out a size specifier of .B, .W, .D
			if (canon[^2] == '.')
			{
				bool match = false;

				switch (canon[^1])
				{
					case 'B':
					{
						size = Constants.Size.Byte;
						match = true;
						break;
					}
					case 'W':
					{
						size = Constants.Size.Word;
						match = true;
						break;
					}
					case 'D':
					{
						size = Constants.Size.Dword;
						match = true;
						break;
					}
				}

				if (match)
				{
					// Remove size, not a part of the mnemonic
					canon = canon[..^2];
				}
			}

			if (!Enum.TryParse(canon, out Constants.Instruction instruction))
			{
				return false;
			}

			result.ResultObject = new(
				t.Line,
				t.Column,
				Constants.StatementType.Instruction,
				t.Lexeme,
				canon
			)
			{
				Instruction = instruction,
				Size = size
			};

			Advance();

			Result<List<Operand>> operandResult = ParseOperands();
			result.Combine(operandResult);
			result.ResultObject.Operands = operandResult.ResultObject;

			return true;
		}

		private bool TryParseDirective(out Result<Statement?> result)
		{
			result = new(null);

			Token t = Current();

			string canon = t.Lexeme.ToUpperInvariant();

			if (canon.StartsWith('.'))
			{
				canon = canon[1..];
			}

			if (!Enum.TryParse(canon, out Constants.Directive directive))
			{
				return false;
			}

			result.ResultObject = new(
				t.Line,
				t.Column,
				Constants.StatementType.Directive,
				t.Lexeme,
				canon
			)
			{
				Directive = directive,
			};

			Advance();

			Result<List<Operand>> operandResult = ParseOperands();
			result.Combine(operandResult);
			result.ResultObject.Operands = operandResult.ResultObject;

			return true;
		}

		private Result<List<Operand>> ParseOperands()
		{
			List<Operand> operands = [];
			Result<List<Operand>> result = new(operands);

			Token t = Current();

			while (t.Type != Constants.TokenType.NewLine && t.Type != Constants.TokenType.EndOfFile)
			{
				Result<Operand> operandResult;

				if (t.Type == Constants.TokenType.LBracket)
				{
					Advance();
					operandResult = ParseMemoryOperand();
				}
				else
				{
					operandResult = ParseOperand();
				}

				result.Combine(operandResult);
				operands.Add(operandResult.ResultObject);

				Advance();
				t = Current();
			}

			return result;
		}

		private Result<Operand> ParseOperand()
		{
			Token t = Current();

			Operand operand = new(t.Line, t.Column, Constants.OperandType.Unknown);
			Result<Operand> result = new(operand);

			if (TryParseRegister(out Result<Operand?> registerResult))
			{
				Debug.Assert(registerResult.ResultObject is not null);
				result.Combine(registerResult);
				result.ResultObject = registerResult.ResultObject;
			}
			else if (TryParseCondition(out Result<Operand?> conditionResult))
			{
				Debug.Assert(conditionResult.ResultObject is not null);
				result.Combine(conditionResult);
				result.ResultObject = conditionResult.ResultObject;
			}
			else if (TryParseSizeOperand(out Result<Operand?> sizeResult))
			{
				Debug.Assert(sizeResult.ResultObject is not null);
				result.Combine(sizeResult);
				result.ResultObject = sizeResult.ResultObject;
			}
			else if (TryParseBlockOperand(out Result<Operand?> blockResult))
			{
				Debug.Assert(blockResult.ResultObject is not null);
				result.Combine(blockResult);
				result.ResultObject = blockResult.ResultObject;
			}

			return result;
		}

		private Result<Operand> ParseMemoryOperand()
		{
			Token t = Current();

			Operand operand = new(t.Line, t.Column, Constants.OperandType.Unknown);
			Result<Operand> result = new(operand);

			bool sawRBracket = false;

			if (IsEndOfOperand(t) || t.Type is Constants.TokenType.RBracket)
			{
				if (t.Type is Constants.TokenType.RBracket)
				{
					sawRBracket = true;
				}

				result.AddError("Parser", $"{t.Line}:{t.Column}:\texpected operand");
			}

			while (!IsEndOfOperand(t))
			{
				if (t.Type is Constants.TokenType.RBracket)
				{
					sawRBracket = true;

					if (!IsEndOfOperand(Next()))
					{
						result.AddError("Parser", $"{t.Line}:{t.Column}:\texpected end of operand after closing bracket");
					}
				}

				Advance();
				t = Current();
			}

			// Advance past end of operand token
			Advance();

			if (!sawRBracket)
			{
				result.AddError("Parser", $"{t.Line}:{t.Column}:\texpected closing bracket in indirect operand");
			}

			return result;
		}

		private bool TryParseRegister(out Result<Operand?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			string canon = t.Lexeme.ToUpperInvariant();

			if (!Enum.TryParse(canon, out Constants.Register register))
			{
				return false;
			}

			result.ResultObject = new(t.Line, t.Column, Constants.OperandType.Register, t.Lexeme, canon)
			{
				Register = register
			};

			Advance();

			return true;
		}

		private bool TryParseCondition(out Result<Operand?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			string canon = t.Lexeme.ToUpperInvariant();

			if (!Enum.TryParse(canon, out Constants.Condition condition))
			{
				return false;
			}

			result.ResultObject = new(t.Line, t.Column, Constants.OperandType.Condition, t.Lexeme, canon)
			{
				Condition = condition
			};

			return true;
		}

		private bool TryParseBlockOperand(out Result<Operand?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			string canon = t.Lexeme.ToUpperInvariant();

			if (!Enum.TryParse(canon, out Constants.BlockOperand blockOperand))
			{
				return false;
			}

			result.ResultObject = new(t.Line, t.Column, Constants.OperandType.BlockOperand, t.Lexeme, canon)
			{
				BlockOperand = blockOperand
			};

			return true;
		}

		private bool TryParseSizeOperand(out Result<Operand?> result)
		{
			result = new(null);

			Token t = Current();

			if (t.Type != Constants.TokenType.Identifier)
			{
				return false;
			}

			string canon = t.Lexeme.ToUpperInvariant();

			if (!Enum.TryParse(canon, out Constants.Size size))
			{
				return false;
			}

			result.ResultObject = new(t.Line, t.Column, Constants.OperandType.Size, t.Lexeme, canon)
			{
				Size = size
			};

			return true;
		}

		private static bool IsEndOfOperand(Token t)
		{
			return t.Type is Constants.TokenType.Unknown
				or Constants.TokenType.Comma
				or Constants.TokenType.NewLine
				or Constants.TokenType.EndOfFile;
		}

		private Token Current()
		{
			if (_position >= _tokens.Count)
			{
				return new(0, 0, Constants.TokenType.EndOfFile);
			}

			return _tokens[_position];
		}

		private Token Next()
		{
			if (_position + 1 >= _tokens.Count)
			{
				return new(0, 0, Constants.TokenType.EndOfFile);
			}

			return _tokens[_position + 1];
		}

		private void Advance()
		{
			_position++;
		}
	}
}