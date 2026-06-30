namespace IM800Asm;

internal class Operand
{
	public int Line { get; set; }
	public int Column { get; set; }
}

internal class RegisterOperand : Operand
{
	public RegisterOperand(int line, int column, Constants.Register register)
	{
		Line = line;
		Column = column;
		Register = register;
	}

	public Constants.Register Register { get; set; }
}

internal class ExpressionOperand : Operand
{
	public ExpressionOperand(int line, int column, List<Token> expressionTokens)
	{
		Line = line;
		Column = column;
		ExpressionTokens = expressionTokens;
	}

	public List<Token> ExpressionTokens { get; set; }
}

internal class IndirectRegisterOperand : Operand
{
	public IndirectRegisterOperand(int line, int column, Constants.Register register)
	{
		Line = line;
		Column = column;
		Register = register;
	}

	public Constants.Register Register { get; set; }
}

internal class IndirectExpressionOperand : Operand
{
	public IndirectExpressionOperand(int line, int column, List<Token> expressionTokens)
	{
		Line = line;
		Column = column;
		ExpressionTokens = expressionTokens;
	}

	public List<Token> ExpressionTokens { get; set; }
}

internal class IndexedOperand : Operand
{
	public IndexedOperand(int line, int column, Constants.Register register, List<Token> expressionTokens)
	{
		Line = line;
		Column = column;
		Register = register;
		ExpressionTokens = expressionTokens;
	}

	public Constants.Register Register { get; set; }
	public List<Token> ExpressionTokens { get; set; }
}

internal class ConditionOperand : Operand
{
	public ConditionOperand(int line, int column, Constants.Condition condition)
	{
		Line = line;
		Column = column;
		Condition = condition;
	}

	public Constants.Condition Condition { get; set; }
}

internal class BlockOperand : Operand
{
	public BlockOperand(int line, int column, Constants.Block block)
	{
		Line = line;
		Column = column;
		Block = block;
	}

	public Constants.Block Block { get; set; }
}

internal class SizeOperand : Operand
{
	public SizeOperand(int line, int column, Constants.Size size)
	{
		Line = line;
		Column = column;
		Size = size;
	}

	public Constants.Size Size { get; set; }
}