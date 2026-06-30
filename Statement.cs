namespace IM800Asm;

internal class Statement
{
	public int Line { get; set; }
	public int Column { get; set; }
}

internal class LabelStatement : Statement
{
	public LabelStatement(int line, int column, string text)
	{
		Line = line;
		Column = column;
		Text = text;
	}

	public string Text { get; set; }
}

internal class InstructionStatement : Statement
{
	public InstructionStatement(int line, int column, Constants.Instruction instruction, Constants.Size? size)
	{
		Line = line;
		Column = column;
		Instruction = instruction;
		Size = size;
	}

	public Constants.Instruction Instruction { get; set; }
	public Constants.Size? Size { get; set; }
}

internal class DirectiveStatement : Statement
{
	public DirectiveStatement(int line, int column, Constants.Directive directive)
	{
		Line = line;
		Column = column;
		Directive = directive;
	}

	public Constants.Directive Directive { get; set; }
}

internal class EndOfFileStatement : Statement
{
	public EndOfFileStatement(int line, int column)
	{
		Line = line;
		Column = column;
	}
}