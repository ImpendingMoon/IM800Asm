using System.Diagnostics;

namespace IM800Asm;

internal partial class Assembler
{
	// ************* MEASURE *****************

	private Result<long> MeasureFormatR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count >= 2);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If it has an immediate operand, add that
		if (st.Operands[1] is ExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatRM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count >= 2);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If it has an indexed operand, add the displacement
		if (st.Operands[0] is IndexedOperand || st.Operands[1] is IndexedOperand)
		{
			result.ResultObject += 2;
		}

		// Invalid if direct + immediate
		if (st.Operands[0] is IndirectExpressionOperand && st.Operands[1] is ExpressionOperand)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\tcannot use a direct destination and immediate source together"
			);
		}

		// If it has a direct operand, add that
		if (st.Operands[0] is IndirectExpressionOperand || st.Operands[1] is IndirectExpressionOperand)
		{
			result.ResultObject += result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
		}

		// If it has an immediate operand, add that
		if (st.Operands[0] is ExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;
		st.Length = result.ResultObject;

		return result;
	}

	private Result<long> MeasureFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 1);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If it has an indexed operand, add the displacement
		if (st.Operands[0] is IndexedOperand || st.Operands[1] is IndexedOperand)
		{
			result.ResultObject += 2;
		}

		// If it has a direct operand, add that
		if (st.Operands[0] is ExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count >= 1);
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If has a direct/relative address, add that
		if (
			(st.Operands.Count == 1 && st.Operands[0] is ExpressionOperand)
			|| (st.Operands.Count == 2 && st.Operands[1] is ExpressionOperand)
		)
		{
			result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		if (st.Instruction == Constants.Instruction.RST)
		{
			result.ResultObject += 2;
		}
		else if (st.Instruction == Constants.Instruction.LD)
		{
			Debug.Assert(st.Operands.Count == 2);
			// LD I
			if (st.Operands[0] is RegisterOperand ro && ro.Register == Constants.Register.I)
			{
				result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
			}
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatSB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(1);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		if (st.Instruction is Constants.Instruction.DJNZ or Constants.Instruction.JAZ or Constants.Instruction.JANZ)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Byte);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private Result<long> MeasureFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;
		st.Length = result.ResultObject;

		return result;
	}

	// ************* EMIT *****************

	private Result<long> EmitFormatR(InstructionStatement st, InstructionTable.Entry entry)
	{
		// LEA has a unique encoding
		if (st.Instruction == Constants.Instruction.LEA)
		{
			return EmitLEA(st, entry);
		}

		Debug.Assert(st.Operands.Count >= 2);

		Result<long> result = new(st.Length);

		int sizeSelector = GetSizeSelectorBits(st.FinalSize);

		var destRegister = (RegisterOperand)st.Operands[0];
		int destRegisterSelector = GetRegisterSelectorBits(destRegister.Register);

		Result<int> srcOperandResult = GetOperandSelector(st, out long? immediateValue, out long? _);
		result.Combine(srcOperandResult);
		int srcRegisterSelector = srcOperandResult.ResultObject;

		int instructionWord = EncodeFormatR(entry.Opcode, sizeSelector, destRegisterSelector, srcRegisterSelector);

		st.FileOffset = _data.Count;

		EmitValue(instructionWord, Constants.Size.Word);

		if (immediateValue is not null)
		{
			EmitValue(immediateValue.Value, st.FinalSize);
		}

		return result;
	}

	private Result<long> EmitFormatRM(InstructionStatement st, InstructionTable.Entry entry)
	{
		// LEA has a unique encoding
		if (st.Instruction == Constants.Instruction.LEA)
		{
			return EmitLEA(st, entry);
		}

		Debug.Assert(st.Operands.Count >= 2);

		Result<long> result = new(st.Length);

		// TODO

		return result;
	}

	private Result<long> EmitFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO

		return result;
	}

	private Result<long> EmitFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO

		return result;
	}

	private Result<long> EmitFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO fix condition from register 

		return result;
	}

	private Result<long> EmitFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO

		return result;
	}

	private Result<long> EmitFormatSB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO

		return result;
	}

	private Result<long> EmitFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		// TODO
		// fix block operands from registers
		// fix size from 1, 2, 4, 8 literals
		// emit

		return result;
	}

	// ************* ENCODE *****************

	private static int EncodeFormatR(int opcode, int sizeSelector, int destRegisterSelector, int srcRegisterSelector)
	{
		const int groupBits = 0b00;

		// 1:0 group (00)
		// 7:2 opcode
		// 9:8 size
		// 12:10 dest register
		// 15:13 src register
		return groupBits
			| (opcode << 2)
			| (sizeSelector << 8)
			| (destRegisterSelector << 10)
			| (srcRegisterSelector << 13);
	}

	private static int EncodeFormatRM(
		int opcode,
		int direction,
		int sizeSelector,
		int registerSelector,
		int addressRegisterSelector
	)
	{
		const int groupBits = 0b01;

		// 1:0 group (01)
		// 6:2 opcode
		// 7 direction
		// 9:8 size
		// 12:10 register
		// 15:13 address register
		return groupBits
			| (opcode << 2)
			| (direction << 7)
			| (sizeSelector << 8)
			| (registerSelector << 10)
			| (addressRegisterSelector << 13);
	}

	private static int EncodeFormatUR(int opcode, int function, int sizeSelector, int registerSelector)
	{
		const int groupBits = 0b10;
		const int subGroupBits = 0b00;

		// 1:0 group (10)
		// 3:2 subgroup (00)
		// 7:4 opcode
		// 9:8 size
		// 12:10 register
		// 15:13 function
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4)
			| (sizeSelector << 8)
			| (registerSelector << 10)
			| (function << 13);
	}

	private static int EncodeFormatUM(int opcode, int function, int sizeSelector, int addressRegisterSelector)
	{
		const int groupBits = 0b10;
		const int subGroupBits = 0b01;

		// 1:0 group (10)
		// 3:2 subgroup (01)
		// 7:4 opcode
		// 9:8 size
		// 12:10 function
		// 15:13 address register
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4)
			| (sizeSelector << 8)
			| (function << 10)
			| (addressRegisterSelector << 13);
	}

	private static int EncodeFormatB(int opcode, int conditionSelector, int addressRegisterSelector)
	{
		const int groupBits = 0b10;
		const int subGroupBits = 0b10;

		// 1:0 group (10)
		// 3:2 subgroup (10)
		// 8:4 opcode
		// 12:9 condition
		// 15:13 address register
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4)
			| (conditionSelector << 9)
			| (addressRegisterSelector << 13);
	}

	private static int EncodeFormatM(int opcode, int function)
	{
		const int groupBits = 0b10;
		const int subGroupBits = 0b11;

		// 1:0 group (10)
		// 3:2 subgroup (11)
		// 7:4 opcode
		// 15:8 function
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4)
			| (function << 8);
	}

	private static int EncodeFormatSB(int opcode)
	{
		const int groupBits = 0b11;
		const int subGroupBits = 0b00;

		// 1:0 group (11)
		// 3:2 subgroup (00)
		// 7:4 opcode
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4);
	}

	private static int EncodeFormatBLK(int opcode, int sizeSelector, int increment, int repeat, int function)
	{
		const int groupBits = 0b11;
		const int subGroupBits = 0b01;

		// 1:0 group (11)
		// 3:2 subgroup (01)
		// 7:4 opcode
		// 9:8 size
		// 10 increment
		// 11 repeat
		// 15:12 function
		return groupBits
			| (subGroupBits << 2)
			| (opcode << 4)
			| (sizeSelector << 8)
			| (increment << 10)
			| (repeat << 11)
			| (function << 12);
	}

	// ************* LEA *****************

	private Result<long> EmitLEA(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 3);

		Result<long> result = new(st.Length);

		// TODO fix size from 1, 2, 4, 8 literals

		return result;
	}

	// ************* HELPERS *****************

	private static Result<Constants.Size> GetInstructionSize(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<Constants.Size> result = new(default);

		// Try to grab the size in priority order:
		// 1. Does the instruction have an explicit size?
		if (st.ManualSize is not null)
		{
			result.ResultObject = st.ManualSize.Value;
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
						result.ResultObject = Constants.Size.Dword;
					}
					else
					{
						result.ResultObject = Constants.Size.Word;
					}
					break;
				}
			}
		}

		// 3. Use the entry default size
		if (result.ResultObject == default)
		{
			result.ResultObject = entry.DefaultSize;
		}

		if (!entry.AllowedSizes.Contains(result.ResultObject))
		{
			result.AddError("Assembler", $"{st.Line}:{st.Column}:\tinvalid size for instruction {st.Instruction}");
		}

		// If there is a meaningful size and the instruction cannot have mixed instruction sizes, check that every
		// register operand size matches the instruction size
		if (result.ResultObject != Constants.Size.Unsized && !entry.AllowMixedSizes)
		{
			foreach (Operand op in st.Operands)
			{
				if (op is RegisterOperand ro)
				{
					if (
						Constants.WideRegisterValues.Contains(ro.Register)
						&& result.ResultObject != Constants.Size.Dword
					)
					{
						result.AddError(
							"Assembler",
							$"{ro.Line}:{ro.Column}:\tcannot use wide register in {result.ResultObject}-sized instruction"
						);
						// Only want one error per instruction here
						break;
					}
					else if (
						Constants.NarrowRegisterValues.Contains(ro.Register)
						&& result.ResultObject == Constants.Size.Dword
					)
					{
						result.AddError(
							"Assembler",
							$"{ro.Line}:{ro.Column}:\tcannot use narrow register in {result.ResultObject}-sized instruction"
						);
						// Only want one error per instruction here
						break;
					}

				}
			}
		}

		return result;
	}

	/// <summary>
	/// Gets the selector bits and expression values of a register or memory operand
	/// </summary>
	/// <param name="st">instruction statement</param>
	/// <param name="immediateValue">value of the immediate or direct expression, if applicable</param>
	/// <param name="displacementValue">value of the displacement expression, if applicable</param>
	/// <returns>result containing the selector bits</returns>
	/// <exception cref="Exception">on unsupported operand type</exception>
	private Result<int> GetOperandSelector(
		InstructionStatement st,
		out long? immediateValue,
		out long? displacementValue
	)
	{
		Result<int> result = new(0);

		immediateValue = null;
		displacementValue = null;

		switch (st.Operands[1])
		{
			case RegisterOperand ro:
			{
				result.ResultObject = GetRegisterSelectorBits(ro.Register);
				break;
			}
			case ExpressionOperand eo:
			{
				result = EncodeImmediateSource(st, eo, out immediateValue);
				break;
			}
			case IndirectRegisterOperand iro:
			{
				result.ResultObject = GetRegisterSelectorBits(iro.Register);
				break;
			}
			case IndirectExpressionOperand ieo:
			{

				break;
			}
			case IndexedOperand idx:
			{
				break;
			}
		}

		return result;
	}

	private Result<int> EncodeImmediateSource(
		InstructionStatement st,
		ExpressionOperand operand,
		out long? immediateValue
	)
	{
		Result<int> result = new(0b111);

		Constants.Size immediateSize;

		if (Constants.BitAndShiftInstructions.Contains(st.Instruction))
		{
			immediateSize = Constants.Size.Byte;
		}
		else
		{
			immediateSize = st.FinalSize;
		}

		Result<long> evalResult = _evaluator.Evaluate(
			operand.ExpressionTokens,
			_locationCounter,
			immediateSize,
			Constants.Signedness.Either
		);

		result.Combine(evalResult);

		immediateValue = evalResult.ResultObject;

		return result;
	}

	private static int GetRegisterSelectorBits(Constants.Register register)
	{
		return register switch
		{
			Constants.Register.A => 0b000,
			Constants.Register.B => 0b001,
			Constants.Register.C => 0b010,
			Constants.Register.D => 0b011,
			Constants.Register.E => 0b100,
			Constants.Register.H => 0b110,
			Constants.Register.L => 0b111,
			Constants.Register.AF => 0b000,
			Constants.Register.BC => 0b001,
			Constants.Register.DE => 0b010,
			Constants.Register.HL => 0b011,
			Constants.Register.IX => 0b100,
			Constants.Register.IY => 0b101,
			Constants.Register.SP => 0b110,
			_ => 0,
		};
	}

	private static int GetSizeSelectorBits(Constants.Size size)
	{
		return size switch
		{
			Constants.Size.Byte => 0b00,
			Constants.Size.Word => 0b01,
			Constants.Size.Dword => 0b10,
			Constants.Size.Qword => 0b11,
			_ => 0,
		};
	}

	private static int GetSizeByteCount(Constants.Size size)
	{
		return size switch
		{
			Constants.Size.Byte => 1,
			Constants.Size.Word => 2,
			Constants.Size.Dword => 4,
			Constants.Size.Qword => 8,
			_ => 0,
		};
	}
}