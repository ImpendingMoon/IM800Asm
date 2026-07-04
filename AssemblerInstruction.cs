namespace IM800Asm;

internal partial class Assembler
{
	private Result<long> MeasureFormatR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> MeasureFormatRM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatRM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}


	private Result<long> MeasureFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}


	private Result<long> MeasureFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}


	private Result<long> MeasureFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}


	private Result<long> MeasureFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}


	private Result<long> MeasureFormatSB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatSB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> MeasureFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private Result<long> EmitFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(0);

		// TODO

		return result;
	}

	private static Result<Constants.Size> GetInstructionSize(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<Constants.Size> result = new(default);

		Constants.Size size = default;

		// Try to grab the size in priority order:
		// 1. Does the instruction have an explicit size?
		if (st.Size is not null)
		{
			size = st.Size.Value;
		}
		// 2. Walk operands, if any is a RegisterOperand use the size of the first register found
		// This prioritizes destination register, but if dest is memory falls back to the source register
		else
		{
			foreach (Operand op in st.Operands)
			{
				if (op is RegisterOperand ro)
				{
					if (Constants.WideRegisterValues.Contains(ro.Register))
					{
						size = Constants.Size.Dword;
					}
					else
					{
						size = Constants.Size.Word;
					}
					break;
				}
			}
		}

		// 3. Use the entry default size
		if (size == default)
		{
			size = entry.DefaultSize;
		}

		if (!entry.AllowedSizes.Contains(size))
		{
			result.AddError("Assembler", $"{st.Line}:{st.Column}:\tinvalid size for instruction {st.Instruction}");
		}

		// If there is a meaningful size and the instruction cannot have mixed instruction sizes, check that every
		// register operand size matches the instruction size
		if (size != Constants.Size.Unsized && !entry.AllowMixedSizes)
		{
			foreach (Operand op in st.Operands)
			{
				if (op is RegisterOperand ro)
				{
					if (Constants.WideRegisterValues.Contains(ro.Register) && size != Constants.Size.Dword)
					{
						result.AddError(
							"Assembler",
							$"{ro.Line}:{ro.Column}:\tcannot use wide register in {size}-sized instruction"
						);
						// Only want one error per instruction here
						break;
					}
					else if (Constants.NarrowRegisterValues.Contains(ro.Register) && size == Constants.Size.Dword)
					{
						result.AddError(
							"Assembler",
							$"{ro.Line}:{ro.Column}:\tcannot use narrow register in {size}-sized instruction"
						);
						// Only want one error per instruction here
						break;
					}

				}
			}
		}

		result.ResultObject = size;
		return result;
	}
}