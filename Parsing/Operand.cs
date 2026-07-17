using System.Text;
using IM800Asm.Core;
using IM800Asm.Lexing;

namespace IM800Asm.Parsing;

internal class Operand
{
	public SourceLocation SourceLocation { get; set; } = new(string.Empty, 0, 0);
}

internal class RegisterOperand : Operand
{
	public RegisterOperand(SourceLocation sourceLocation, Constants.Register register)
	{
		SourceLocation = sourceLocation;
		Register = register;
	}

	public Constants.Register Register { get; set; }

	public override string ToString()
	{
		return Register.ToString();
	}
}

internal class ExpressionOperand : Operand
{
	public ExpressionOperand(SourceLocation sourceLocation, List<Token> expressionTokens)
	{
		SourceLocation = sourceLocation;
		ExpressionTokens = expressionTokens;
	}

	public List<Token> ExpressionTokens { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();

		foreach (Token token in ExpressionTokens)
		{
			sb.Append(token.ToShortString());
		}

		return sb.ToString();
	}
}

internal class IndirectRegisterOperand : Operand
{
	public IndirectRegisterOperand(SourceLocation sourceLocation, Constants.Register register)
	{
		SourceLocation = sourceLocation;
		Register = register;
	}

	public Constants.Register Register { get; set; }

	public override string ToString()
	{
		return $"[{Register}]";
	}
}

internal class IndirectExpressionOperand : Operand
{
	public IndirectExpressionOperand(SourceLocation sourceLocation, List<Token> expressionTokens)
	{
		SourceLocation = sourceLocation;
		ExpressionTokens = expressionTokens;
	}

	public List<Token> ExpressionTokens { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append('[');

		foreach (Token token in ExpressionTokens)
		{
			sb.Append(token.ToShortString());
		}

		sb.Append(']');

		return sb.ToString();
	}
}

internal class IndexedOperand : Operand
{
	public IndexedOperand(SourceLocation sourceLocation, Constants.Register register, List<Token> expressionTokens)
	{
		SourceLocation = sourceLocation;
		Register = register;
		ExpressionTokens = expressionTokens;
	}

	public Constants.Register Register { get; set; }
	public List<Token> ExpressionTokens { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append('[');
		sb.Append(Register.ToString());

		foreach (Token token in ExpressionTokens)
		{
			sb.Append(token.ToShortString());
		}

		sb.Append(']');

		return sb.ToString();
	}
}

internal class ConditionOperand : Operand
{
	public ConditionOperand(SourceLocation sourceLocation, Constants.Condition condition)
	{
		SourceLocation = sourceLocation;
		Condition = condition;
	}

	public Constants.Condition Condition { get; set; }

	public override string ToString()
	{
		return Condition.ToString();
	}
}

internal class BlockOperand : Operand
{
	public BlockOperand(SourceLocation sourceLocation, Constants.Block block)
	{
		SourceLocation = sourceLocation;
		Block = block;
	}

	public Constants.Block Block { get; set; }

	public override string ToString()
	{
		return Block.ToString();
	}
}

internal class SizeOperand : Operand
{
	public SizeOperand(SourceLocation sourceLocation, Constants.Size size)
	{
		SourceLocation = sourceLocation;
		Size = size;
	}

	public Constants.Size Size { get; set; }

	public override string ToString()
	{
		return Size.ToString();
	}
}
