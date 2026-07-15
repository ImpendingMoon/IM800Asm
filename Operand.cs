using System.Text;

namespace IM800Asm;

internal class Operand
{
	public Location Location { get; set; }
}

internal class RegisterOperand : Operand
{
	public RegisterOperand(Location location, Constants.Register register)
	{
		Location = location;
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
	public ExpressionOperand(Location location, List<Token> expressionTokens)
	{
		Location = location;
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
	public IndirectRegisterOperand(Location location, Constants.Register register)
	{
		Location = location;
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
	public IndirectExpressionOperand(Location location, List<Token> expressionTokens)
	{
		Location = location;
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
	public IndexedOperand(Location location, Constants.Register register, List<Token> expressionTokens)
	{
		Location = location;
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
	public ConditionOperand(Location location, Constants.Condition condition)
	{
		Location = location;
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
	public BlockOperand(Location location, Constants.Block block)
	{
		Location = location;
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
	public SizeOperand(Location location, Constants.Size size)
	{
		Location = location;
		Size = size;
	}

	public Constants.Size Size { get; set; }

	public override string ToString()
	{
		return Size.ToString();
	}
}