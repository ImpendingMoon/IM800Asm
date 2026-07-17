using System.Text;
using IM800Asm.Core;

namespace IM800Asm.Parsing;

internal class Statement
{
	public SourceLocation SourceLocation { get; set; } = new(string.Empty, 0, 0);

	// Used to detect differences between pass 1 and pass 2
	public long MeasuredLocationCounter { get; set; }

	// Used for listing files
	public bool EmitsData { get; set; }
	public long FileOffset { get; set; }
	public long Length { get; set; }
}

internal class LabelStatement : Statement
{
	public LabelStatement(SourceLocation sourceLocation, string text)
	{
		SourceLocation = sourceLocation;
		Text = text;
	}

	public string Text { get; set; }

	public override string ToString()
	{
		return $"{SourceLocation} Label: {Text}";
	}
}

internal class InstructionStatement : Statement
{
	public InstructionStatement(SourceLocation sourceLocation, Constants.Instruction instruction, Constants.Size? size)
	{
		SourceLocation = sourceLocation;
		Instruction = instruction;
		ManualSize = size;
		FinalSize = default;
	}

	public Constants.Instruction Instruction { get; set; }
	public Constants.Size? ManualSize { get; set; }
	public List<Operand> Operands { get; set; } = [];

	/// <summary>
	///     Size as resolved by the Pass 1 Assembler
	/// </summary>
	public Constants.Size FinalSize { get; set; }

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append($"{SourceLocation} {Instruction}");

		if (ManualSize is not null)
		{
			sb.Append('.');
			sb.Append(ManualSize.ToString()![0]);
		}

		sb.Append(' ');

		for (int i = 0; i < Operands.Count; i++)
		{
			sb.Append(Operands[i]);

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
	public DirectiveStatement(SourceLocation sourceLocation, Constants.Directive directive)
	{
		SourceLocation = sourceLocation;
		Directive = directive;
	}

	public Constants.Directive Directive { get; set; }
	public List<Operand> Operands { get; set; } = [];

	public override string ToString()
	{
		StringBuilder sb = new();

		sb.Append($"{SourceLocation} {Directive}");

		sb.Append(' ');

		for (int i = 0; i < Operands.Count; i++)
		{
			sb.Append(Operands[i]);

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
	public EndOfFileStatement(SourceLocation sourceLocation)
	{
		SourceLocation = sourceLocation;
	}

	public override string ToString()
	{
		return $"{SourceLocation} EOF";
	}
}
