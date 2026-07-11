using System.Text;

namespace IM800Asm;

internal class Statement
{
	public Location Location { get; set; }

	// Used to detect differences between pass 1 and pass 2
	public long MeasuredLocationCounter { get; set; }

	// Used for listing files
	public long FileOffset { get; set; }
	public long Length { get; set; }
}

internal class LabelStatement : Statement
{
	public LabelStatement(Location location, string text)
	{
		Location = location;
		Text = text;
	}

	public string Text { get; set; }

	public override string ToString()
	{
		return $"{Location} Label: {Text}";
	}
}

internal class InstructionStatement : Statement
{
	public InstructionStatement(Location location, Constants.Instruction instruction, Constants.Size? size)
	{
		Location = location;
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

		sb.Append($"{Location} {Instruction}");

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
	public DirectiveStatement(Location location, Constants.Directive directive)
	{
		Location = location;
		Directive = directive;
	}

	public Constants.Directive Directive { get; set; }
	public List<Operand> Operands { get; set; } = [];

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append($"{Location} {Directive}");

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
	public EndOfFileStatement(Location location)
	{
		Location = location;
	}

	public override string ToString()
	{
		return $"{Location} EOF";
	}
}