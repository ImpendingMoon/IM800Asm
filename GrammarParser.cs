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

		private Token Current()
		{
			if (_position >= _tokens.Count)
			{
				return new(0, 0, Constants.TokenType.EndOfFile);
			}

			return _tokens[_position];
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

			// TODO: ParseOperands

			return true;
		}

		private void Advance()
		{
			_position++;
		}
	}
}