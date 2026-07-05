using System.Text;

namespace IM800Asm;

internal class Statement
{
	public int Line { get; set; }
	public int Column { get; set; }

	// Used for listing files
	public long FileOffset { get; set; }
	public long Length { get; set; }
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

	public override string ToString()
	{
		return $"{Line}:{Column}: Label: {Text}";
	}
}

internal class InstructionStatement : Statement
{
	public InstructionStatement(int line, int column, Constants.Instruction instruction, Constants.Size? size)
	{
		Line = line;
		Column = column;
		Instruction = instruction;
		ManualSize = size;
		FinalSize = default;
	}

	public Constants.Instruction Instruction { get; set; }
	public Constants.Size? ManualSize { get; set; }
	public List<Operand> Operands { get; set; } = [];

	/// <summary>
	/// Size as resolved by the Pass 1 Assembler
	/// </summary>
	public Constants.Size FinalSize { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append($"{Line}:{Column}: {Instruction}");

		if (ManualSize is not null)
		{
			sb.Append('.');
			sb.Append(ManualSize.ToString()![0]);
		}

		sb.Append(' ');

		for (int i = 0; i < Operands.Count; i++)
		{
			sb.Append(Operands[i].ToString());

			if (i != Operands.Count - 1)
			{
				sb.Append(", ");
			}
		}

		return sb.ToString();
	}
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
	public List<Operand> Operands { get; set; } = [];

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append($"{Line}:{Column}: {Directive}");

		sb.Append(' ');

		for (int i = 0; i < Operands.Count; i++)
		{
			sb.Append(Operands[i].ToString());

			if (i != Operands.Count - 1)
			{
				sb.Append(", ");
			}
		}

		return sb.ToString();
	}
}

internal class EndOfFileStatement : Statement
{
	public EndOfFileStatement(int line, int column)
	{
		Line = line;
		Column = column;
	}

	public override string ToString()
	{
		return $"{Line}:{Column}: EOF";
	}
}