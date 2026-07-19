using System.Diagnostics;
using IM800Asm.Core;
using IM800Asm.Lexing;
using IM800Asm.Parsing;

namespace IM800Asm.Assembly;

internal partial class Assembler
{
	// Fixes Condition operands and block operands in a statement
	// since they get matched as registers in the lexer
	private static Result FixupInstruction(InstructionStatement st, InstructionTable.Entry entry)
	{
		var result = new Result();

		if (entry.InstructionFormat == Constants.InstructionFormat.B)
		{
			bool hasCondition = st.Operands.Count == 2
				|| st is { Instruction: Constants.Instruction.RET, Operands.Count: 1 };

			if (hasCondition && st.Operands[0] is RegisterOperand ro)
			{
				if (ro.Register == Constants.Register.C)
				{
					st.Operands[0] = new ConditionOperand(ro.SourceLocation, Constants.Condition.C);
				}
				else if (ro.Register == Constants.Register.L)
				{
					st.Operands[0] = new ConditionOperand(ro.SourceLocation, Constants.Condition.L);
				}
				else
				{
					result.AddError(
						st.SourceLocation,
						Constants.ErrorCode.InvalidOperand,
						$"invalid operand for instruction {st.Instruction}"
					);
				}
			}
		}
		else if (entry.InstructionFormat == Constants.InstructionFormat.BLK)
		{
			for (int i = 0; i < 2; i++)
			{
				if (st.Operands[i] is RegisterOperand ro)
				{
					if (ro.Register == Constants.Register.I)
					{
						st.Operands[i] = new BlockOperand(ro.SourceLocation, Constants.Block.I);
					}
					else if (ro.Register == Constants.Register.D)
					{
						st.Operands[i] = new BlockOperand(ro.SourceLocation, Constants.Block.D);
					}
					else if (ro.Register == Constants.Register.R)
					{
						st.Operands[i] = new BlockOperand(ro.SourceLocation, Constants.Block.R);
					}
					else
					{
						result.AddError(
							st.SourceLocation,
							Constants.ErrorCode.InvalidOperand,
							$"invalid operand for instruction {st.Instruction}"
						);
					}
				}
			}
		}

		return result;
	}

	// ************* MEASURE *****************

	private static Result<long> MeasureFormatR(InstructionStatement st, InstructionTable.Entry entry)
	{
		// LEA has a unique encoding and a fixed word-sized source, regardless of size
		if (st.Instruction == Constants.Instruction.LEA)
		{
			return MeasureLEA(st);
		}

		Debug.Assert(st.Operands.Count >= 2);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If it has an immediate operand, add that
		if (st.Operands[1] is ExpressionOperand)
		{
			if (Constants.BitAndShiftInstructions.Contains(st.Instruction))
			{
				result.ResultObject += GetSizeByteCount(Constants.Size.Byte);
			}
			else
			{
				result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
			}
		}

		st.Length = result.ResultObject;
		return result;
	}

	private static Result<long> MeasureFormatRM(InstructionStatement st, InstructionTable.Entry entry)
	{
		// LEA has a unique encoding and a fixed word-sized source, regardless of size
		if (st.Instruction == Constants.Instruction.LEA)
		{
			return MeasureLEA(st);
		}

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
				st.SourceLocation,
				Constants.ErrorCode.InvalidAddressingMode,
				"cannot use a direct destination and immediate source together"
			);
		}

		// If it has a direct operand, add that
		if (st.Operands[0] is IndirectExpressionOperand || st.Operands[1] is IndirectExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
		}

		// If it has an immediate operand, add that
		if (st.Operands[1] is ExpressionOperand)
		{
			if (Constants.BitAndShiftInstructions.Contains(st.Instruction))
			{
				result.ResultObject += GetSizeByteCount(Constants.Size.Byte);
			}
			else
			{
				result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
			}
		}

		st.Length = result.ResultObject;
		return result;
	}

	// LEA's size field is repurposed as a scale factor, not an instruction size. The destination
	// is always a dword and the source is always a word.
	private static Result<long> MeasureLEA(InstructionStatement st)
	{
		Debug.Assert(st.Operands.Count == 3);

		Result<long> result = new(2);

		st.FinalSize = Constants.Size.Dword;

		if (st.ManualSize is not null)
		{
			result.AddError(st.SourceLocation, Constants.ErrorCode.InvalidSize, "LEA does not support a size suffix");
		}

		Operand destOperand = st.Operands[0];
		Operand srcOperand = st.Operands[1];

		// If it has an indexed operand, add the displacement
		if (destOperand is IndexedOperand || srcOperand is IndexedOperand)
		{
			result.ResultObject += 2;
		}

		// If it has a direct operand, add the 32-bit address
		if (destOperand is IndirectExpressionOperand || srcOperand is IndirectExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
		}

		// The source is always word-sized when it's an immediate, regardless of the scale operand
		if (srcOperand is ExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Word);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private static Result<long> MeasureFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 1);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);

		// PUSH #
		if (st.Operands[0] is ExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
		}

		st.FinalSize = sizeResult.ResultObject;
		st.Length = result.ResultObject;

		return result;
	}

	private static Result<long> MeasureFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 1);

		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If it has an indexed operand, add the displacement
		if (st.Operands[0] is IndexedOperand)
		{
			result.ResultObject += 2;
		}

		// If it has a direct operand, add that
		if (st.Operands[0] is IndirectExpressionOperand)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private static Result<long> MeasureFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		// If has a direct/relative address, add that
		if (
			st.Operands is [ExpressionOperand] or [_, ExpressionOperand]
		)
		{
			result.ResultObject += GetSizeByteCount(sizeResult.ResultObject);
		}

		st.Length = result.ResultObject;
		return result;
	}

	private static Result<long> MeasureFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(2);

		Result<Constants.Size> sizeResult = GetInstructionSize(st, entry);
		result.Combine(sizeResult);
		st.FinalSize = sizeResult.ResultObject;

		if (st.Instruction is Constants.Instruction.RST or Constants.Instruction.BKPT)
		{
			result.ResultObject += GetSizeByteCount(Constants.Size.Byte);
		}
		else if (st.Instruction == Constants.Instruction.LD)
		{
			Debug.Assert(st.Operands.Count == 2);
			// LD I
			if (st.Operands[0] is RegisterOperand { Register: Constants.Register.I })
			{
				result.ResultObject += GetSizeByteCount(Constants.Size.Dword);
			}
		}

		st.Length = result.ResultObject;
		return result;
	}

	private static Result<long> MeasureFormatSB(InstructionStatement st, InstructionTable.Entry entry)
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

	private static Result<long> MeasureFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
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

		Result<int> srcOperandResult = GetSourceOperandSelector(
			st,
			out long? immediateValue,
			out Constants.Size immediateSize,
			out long? _
		);
		result.Combine(srcOperandResult);
		int srcRegisterSelector = srcOperandResult.ResultObject;

		int instructionWord = EncodeFormatR(entry.Opcode, sizeSelector, destRegisterSelector, srcRegisterSelector);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		if (immediateValue is not null)
		{
			EmitValue(immediateValue.Value, immediateSize);
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

		int sizeSelector = GetSizeSelectorBits(st.FinalSize);

		Operand opA = st.Operands[0];
		Operand opB = st.Operands[1];

		bool memoryIsFirst = IsMemoryOperand(opA);
		Operand memoryOperand = memoryIsFirst ? opA : opB;
		Operand otherOperand = memoryIsFirst ? opB : opA;

		// 0 = Load (Memory to Register), 1 = Store (Register to Memory)
		int direction = memoryIsFirst ? 1 : 0;

		Result<int> addressResult = GetMemoryOperandSelector(
			memoryOperand,
			out long? displacementValue,
			out long? directAddressValue
		);
		result.Combine(addressResult);
		int addressRegisterSelector = addressResult.ResultObject;

		int registerSelector;
		long? immediateValue = null;
		Constants.Size immediateSize = default;

		switch (otherOperand)
		{
			case RegisterOperand ro:
			{
				registerSelector = GetRegisterSelectorBits(ro.Register);
				break;
			}
			case ExpressionOperand eo:
			{
				Result<int> immResult = GetImmediateOperand(st, eo, out immediateValue, out immediateSize);
				result.Combine(immResult);
				registerSelector = immResult.ResultObject;
				break;
			}
			default:
				throw new Exception($"Unexpected operand type {otherOperand.GetType()} in format RM");
		}

		int instructionWord = EncodeFormatRM(
			entry.Opcode,
			direction,
			sizeSelector,
			registerSelector,
			addressRegisterSelector
		);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		// Displacement is always emitted before an immediate value
		if (displacementValue is not null)
		{
			EmitValue(displacementValue.Value, Constants.Size.Word);
		}

		if (directAddressValue is not null)
		{
			EmitValue(directAddressValue.Value, Constants.Size.Dword);
		}

		if (immediateValue is not null)
		{
			EmitValue(immediateValue.Value, immediateSize);
		}

		return result;
	}

	private Result<long> EmitFormatUR(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 1);

		Result<long> result = new(st.Length);

		int sizeSelector = GetSizeSelectorBits(st.FinalSize);

		int registerSelector;
		long? immediateValue = null;
		Constants.Size immediateSize = default;

		switch (st.Operands[0])
		{
			case RegisterOperand ro:
			{
				registerSelector = GetRegisterSelectorBits(ro.Register);
				break;
			}
			case ExpressionOperand eo:
			{
				Result<int> immResult = GetImmediateOperand(st, eo, out immediateValue, out immediateSize);
				result.Combine(immResult);
				registerSelector = immResult.ResultObject;
				break;
			}
			default:
				throw new Exception($"Unexpected operand type {st.Operands[0].GetType()} in format UR");
		}

		int instructionWord = EncodeFormatUR(entry.Opcode, entry.Function, sizeSelector, registerSelector);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		if (immediateValue is not null)
		{
			EmitValue(immediateValue.Value, immediateSize);
		}

		return result;
	}

	private Result<long> EmitFormatUM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 1);

		Result<long> result = new(st.Length);

		int sizeSelector = GetSizeSelectorBits(st.FinalSize);

		Result<int> addressResult = GetMemoryOperandSelector(
			st.Operands[0],
			out long? displacementValue,
			out long? directAddressValue
		);
		result.Combine(addressResult);
		int addressRegisterSelector = addressResult.ResultObject;

		int instructionWord = EncodeFormatUM(entry.Opcode, entry.Function, sizeSelector, addressRegisterSelector);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		if (displacementValue is not null)
		{
			EmitValue(displacementValue.Value, Constants.Size.Word);
		}

		if (directAddressValue is not null)
		{
			EmitValue(directAddressValue.Value, Constants.Size.Dword);
		}

		return result;
	}

	private Result<long> EmitFormatB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		bool isRelative = st.Instruction is Constants.Instruction.JR or Constants.Instruction.CR;

		// JR and CR have 1 entry but use different opcodes for sizes
		int opcode = entry.Opcode;
		if (isRelative && st.FinalSize == Constants.Size.Word)
		{
			opcode += 1;
		}

		if (st.Instruction == Constants.Instruction.RET)
		{
			int conditionSelector = Constants.AlwaysConditionSelector;

			if (st.Operands.Count == 1)
			{
				Result<int> condResult = GetConditionSelector(st.Operands[0]);
				result.Combine(condResult);
				conditionSelector = condResult.ResultObject;
			}

			// address should be 0 on return
			int retWord = EncodeFormatB(opcode, conditionSelector, 0);

			st.FileOffset = _data.Count;
			st.EmitsData = true;
			EmitValue(retWord, Constants.Size.Word);

			return result;
		}

		int addressOperandIndex = 0;
		int conditionSelectorValue = Constants.AlwaysConditionSelector;

		if (st.Operands.Count == 2)
		{
			Result<int> condResult = GetConditionSelector(st.Operands[0]);
			result.Combine(condResult);
			conditionSelectorValue = condResult.ResultObject;
			addressOperandIndex = 1;
		}

		Operand targetOperand = st.Operands[addressOperandIndex];

		int addressRegisterSelector;
		long? valueToEmit = null;
		var valueSize = Constants.Size.Unsized;

		switch (targetOperand)
		{
			case RegisterOperand ro:
			{
				addressRegisterSelector = GetRegisterSelectorBits(ro.Register);
				break;
			}
			case ExpressionOperand eo:
			{
				addressRegisterSelector = 0b111;

				if (isRelative)
				{
					Result<long> relResult = EvaluateRelativeOperand(eo, st.Length, st.FinalSize);
					result.Combine(relResult);
					valueToEmit = relResult.ResultObject;
					valueSize = st.FinalSize;
				}
				else
				{
					Result<long> evalResult = _evaluator.Evaluate(
						eo.ExpressionTokens,
						_locationCounter,
						Constants.Size.Dword,
						Constants.Signedness.Unsigned
					);
					result.Combine(evalResult);
					valueToEmit = evalResult.ResultObject;
					valueSize = Constants.Size.Dword;
				}

				break;
			}
			default:
			{
				throw new Exception($"Unexpected operand type {targetOperand.GetType()} in format B");
			}
		}

		int instructionWord = EncodeFormatB(opcode, conditionSelectorValue, addressRegisterSelector);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		if (valueToEmit is not null)
		{
			EmitValue(valueToEmit.Value, valueSize);
		}

		return result;
	}

	private Result<long> EmitFormatM(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		int function = entry.Function;
		long? extraValue = null;
		var extraSize = Constants.Size.Unsized;

		if (st.Instruction is Constants.Instruction.RST or Constants.Instruction.BKPT)
		{
			Debug.Assert(st.Operands is [ExpressionOperand]);
			var eo = (ExpressionOperand)st.Operands[0];

			Result<long> evalResult = _evaluator.Evaluate(
				eo.ExpressionTokens,
				_locationCounter,
				Constants.Size.Byte,
				Constants.Signedness.Unsigned
			);
			result.Combine(evalResult);

			extraValue = evalResult.ResultObject;
			extraSize = Constants.Size.Byte;
		}
		else if (st.Instruction == Constants.Instruction.IM)
		{
			// IM has one entry but two opcodes
			Debug.Assert(st.Operands is [ExpressionOperand]);
			var eo = (ExpressionOperand)st.Operands[0];

			if (eo.ExpressionTokens is [NumberToken nt])
			{
				switch (nt.Value)
				{
					case 1:
						function = 0b00000100;
						break;
					case 2:
						function = 0b00000101;
						break;
					default:
						result.AddError(
							st.SourceLocation,
							Constants.ErrorCode.InvalidOperand,
							"IM mode must be 1 or 2"
						);
						break;
				}
			}
			else
			{
				result.AddError(
					st.SourceLocation,
					Constants.ErrorCode.InvalidOperand,
					"IM mode must be 1 or 2"
				);
			}
		}
		else if (
			st is
			{
				Instruction: Constants.Instruction.LD, Operands: [RegisterOperand { Register: Constants.Register.I }, _]
			}
		)
		{
			var eo = (ExpressionOperand)st.Operands[1];

			Result<long> evalResult = _evaluator.Evaluate(
				eo.ExpressionTokens,
				_locationCounter,
				Constants.Size.Dword,
				Constants.Signedness.Unsigned
			);
			result.Combine(evalResult);

			extraValue = evalResult.ResultObject;
			extraSize = Constants.Size.Dword;
		}

		int instructionWord = EncodeFormatM(entry.Opcode, function);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		if (extraValue is not null)
		{
			EmitValue(extraValue.Value, extraSize);
		}

		return result;
	}

	private Result<long> EmitFormatSB(InstructionStatement st, InstructionTable.Entry entry)
	{
		Result<long> result = new(st.Length);

		long? relativeValue = null;

		if (st.Operands is [ExpressionOperand eo])
		{
			Result<long> relResult = EvaluateRelativeOperand(eo, st.Length, Constants.Size.Byte);
			result.Combine(relResult);
			relativeValue = relResult.ResultObject;
		}

		int instructionByte = EncodeFormatSB(entry.Opcode);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionByte, Constants.Size.Byte);

		if (relativeValue is not null)
		{
			EmitValue(relativeValue.Value, Constants.Size.Byte);
		}

		return result;
	}

	private Result<long> EmitFormatBLK(InstructionStatement st, InstructionTable.Entry entry)
	{
		Debug.Assert(st.Operands.Count == 3);

		Result<long> result = new(st.Length);

		int incrementBit = GetIncrementBit(st.Operands[0]);
		int repeatBit = GetRepeatBit(st.Operands[1]);

		Result<int> sizeResult = GetBlockSizeSelector(st.Operands[2]);
		result.Combine(sizeResult);
		int sizeSelector = sizeResult.ResultObject;

		int instructionWord = EncodeFormatBLK(entry.Opcode, sizeSelector, incrementBit, repeatBit, entry.Function);

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

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

	private static int EncodeFormatRM
	(
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

		int scaleSelector = GetScaleOperandSelector(st.Operands[2]);

		Operand destOperand = st.Operands[0];
		Operand srcOperand = st.Operands[1];

		int instructionWord;
		long? displacementValue = null;
		long? directAddressValue = null;
		long? immediateValue = null;

		if (entry.InstructionFormat == Constants.InstructionFormat.R)
		{
			// destOperand is always the wide register destination in this form
			var destRegister = (RegisterOperand)destOperand;
			int destRegisterSelector = GetRegisterSelectorBits(destRegister.Register);

			int srcRegisterSelector;

			switch (srcOperand)
			{
				case RegisterOperand ro:
				{
					srcRegisterSelector = GetRegisterSelectorBits(ro.Register);
					break;
				}
				case ExpressionOperand eo:
				{
					Result<long> evalResult = _evaluator.Evaluate(
						eo.ExpressionTokens,
						_locationCounter,
						Constants.Size.Word,
						Constants.Signedness.Either
					);
					result.Combine(evalResult);
					immediateValue = evalResult.ResultObject;
					srcRegisterSelector = 0b111;
					break;
				}
				default:
					throw new Exception($"Unexpected operand type {srcOperand.GetType()} in LEA format R");
			}

			instructionWord = EncodeFormatR(entry.Opcode, scaleSelector, destRegisterSelector, srcRegisterSelector);
		}
		else
		{
			bool memoryIsFirst = IsMemoryOperand(destOperand);
			Operand memoryOperand = memoryIsFirst ? destOperand : srcOperand;
			Operand otherOperand = memoryIsFirst ? srcOperand : destOperand;
			int direction = memoryIsFirst ? 1 : 0;

			Result<int> addressResult = GetMemoryOperandSelector(
				memoryOperand,
				out displacementValue,
				out directAddressValue
			);
			result.Combine(addressResult);
			int addressRegisterSelector = addressResult.ResultObject;

			int registerSelector;

			switch (otherOperand)
			{
				case RegisterOperand ro:
				{
					registerSelector = GetRegisterSelectorBits(ro.Register);
					break;
				}
				case ExpressionOperand eo:
				{
					Result<long> evalResult = _evaluator.Evaluate(
						eo.ExpressionTokens,
						_locationCounter,
						Constants.Size.Word,
						Constants.Signedness.Either
					);
					result.Combine(evalResult);
					immediateValue = evalResult.ResultObject;
					registerSelector = 0b111;
					break;
				}
				default:
					throw new Exception($"Unexpected operand type {otherOperand.GetType()} in LEA format RM");
			}

			instructionWord = EncodeFormatRM(
				entry.Opcode,
				direction,
				scaleSelector,
				registerSelector,
				addressRegisterSelector
			);
		}

		st.FileOffset = _data.Count;
		st.EmitsData = true;
		EmitValue(instructionWord, Constants.Size.Word);

		// Displacement is always emitted before an immediate value
		if (displacementValue is not null)
		{
			EmitValue(displacementValue.Value, Constants.Size.Word);
		}

		if (directAddressValue is not null)
		{
			EmitValue(directAddressValue.Value, Constants.Size.Dword);
		}

		if (immediateValue is not null)
		{
			EmitValue(immediateValue.Value, Constants.Size.Word);
		}

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
				if (op is not RegisterOperand ro)
				{
					continue;
				}

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

		// 3. Use the entry default size
		if (result.ResultObject == default)
		{
			result.ResultObject = entry.DefaultSize;
		}

		if (!entry.AllowedSizes.Contains(result.ResultObject))
		{
			result.AddError(
				st.SourceLocation,
				Constants.ErrorCode.InvalidSize,
				$"invalid size for instruction {st.Instruction}"
			);
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
							ro.SourceLocation,
							Constants.ErrorCode.OperandSizeMismatch,
							$"cannot use wide register in {result.ResultObject}-sized instruction"
						);
						// Only want one error per instruction here
						break;
					}

					if (
						Constants.NarrowRegisterValues.Contains(ro.Register)
						&& result.ResultObject == Constants.Size.Dword
					)
					{
						result.AddError(
							ro.SourceLocation,
							Constants.ErrorCode.OperandSizeMismatch,
							$"cannot use narrow register in {result.ResultObject}-sized instruction"
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
	///     Gets the selector bits and expression values of a register or memory operand
	/// </summary>
	/// <param name="st">instruction statement</param>
	/// <param name="immediateValue">value of the immediate or direct expression, if applicable</param>
	/// <param name="immediateSize">size of the immediate or direct expression, if applicable</param>
	/// <param name="displacementValue">value of the displacement expression, if applicable</param>
	/// <returns>result containing the selector bits</returns>
	/// <exception cref="Exception">on unsupported operand type</exception>
	private Result<int> GetSourceOperandSelector
	(
		InstructionStatement st,
		out long? immediateValue,
		out Constants.Size immediateSize,
		out long? displacementValue
	)
	{
		Result<int> result = new(0);

		immediateValue = null;
		immediateSize = Constants.Size.Unsized;
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
				result = GetImmediateOperand(st, eo, out immediateValue, out immediateSize);
				break;
			}
			case IndirectRegisterOperand iro:
			{
				result.ResultObject = GetRegisterSelectorBits(iro.Register);
				break;
			}
			case IndirectExpressionOperand ieo:
			{
				result = GetDirectOperand(ieo, out immediateValue);
				break;
			}
			case IndexedOperand idx:
			{
				result = GetIndexedOperand(idx, out displacementValue);
				break;
			}
		}

		return result;
	}

	private Result<int> GetImmediateOperand
	(
		InstructionStatement st,
		ExpressionOperand operand,
		out long? immediateValue,
		out Constants.Size immediateSize
	)
	{
		Result<int> result = new(0b111);

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

	private Result<int> GetDirectOperand(IndirectExpressionOperand operand, out long? immediateValue)
	{
		Result<int> result = new(0b111);

		Result<long> evalResult = _evaluator.Evaluate(
			operand.ExpressionTokens,
			_locationCounter,
			Constants.Size.Dword,
			Constants.Signedness.Unsigned
		);

		result.Combine(evalResult);

		immediateValue = evalResult.ResultObject;

		return result;
	}

	private Result<int> GetIndexedOperand(IndexedOperand operand, out long? displacementValue)
	{
		Result<int> result = new(GetRegisterSelectorBits(operand.Register));

		Result<long> evalResult = _evaluator.Evaluate(
			operand.ExpressionTokens,
			_locationCounter,
			Constants.Size.Word,
			Constants.Signedness.Signed
		);

		result.Combine(evalResult);
		displacementValue = evalResult.ResultObject;

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
			Constants.Register.H => 0b101,
			Constants.Register.L => 0b110,
			Constants.Register.AF => 0b000,
			Constants.Register.BC => 0b001,
			Constants.Register.DE => 0b010,
			Constants.Register.HL => 0b011,
			Constants.Register.IX => 0b100,
			Constants.Register.IY => 0b101,
			Constants.Register.SP => 0b110,
			_ => 0
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
			_ => 0
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
			_ => 0
		};
	}

	private static bool IsMemoryOperand(Operand operand)
	{
		return operand is IndirectRegisterOperand or IndirectExpressionOperand or IndexedOperand;
	}

	/// <summary>
	///     Gets the address register selector bits for a memory (Indirect/Indexed/Direct) operand
	/// </summary>
	/// <param name="operand">memory operand</param>
	/// <param name="displacementValue">value of the displacement expression, if the operand is indexed</param>
	/// <param name="directAddressValue">value of the address expression, if the operand is direct</param>
	/// <returns>result containing the selector bits</returns>
	private Result<int> GetMemoryOperandSelector
		(Operand operand, out long? displacementValue, out long? directAddressValue)
	{
		Result<int> result = new(0);

		displacementValue = null;
		directAddressValue = null;

		switch (operand)
		{
			case IndirectRegisterOperand iro:
			{
				result.ResultObject = GetRegisterSelectorBits(iro.Register);
				break;
			}
			case IndexedOperand idx:
			{
				result = GetIndexedOperand(idx, out displacementValue);
				break;
			}
			case IndirectExpressionOperand ieo:
			{
				result = GetDirectOperand(ieo, out directAddressValue);
				break;
			}
			default:
				throw new Exception($"Unexpected operand type {operand.GetType()} for a memory operand");
		}

		return result;
	}

	private static Result<int> GetConditionSelector(Operand operand)
	{
		Result<int> result = new(Constants.AlwaysConditionSelector);

		if (operand is ConditionOperand co)
		{
			result.ResultObject = (int)co.Condition;
		}

		return result;
	}

	/// <summary>
	///     Evaluates a PC-relative operand. The expression is evaluated as an absolute value and then subtracts the location
	///     counter after this instruction
	/// </summary>
	/// <param name="operand">the target address expression</param>
	/// <param name="instructionLength">total length of this instruction, in bytes</param>
	/// <param name="size">size of the relative offset to emit</param>
	private Result<long> EvaluateRelativeOperand(ExpressionOperand operand, long instructionLength, Constants.Size size)
	{
		Result<long> result = new(0);

		Result<long> evalResult = _evaluator.Evaluate(
			operand.ExpressionTokens,
			_locationCounter,
			Constants.Size.Qword,
			Constants.Signedness.Signed
		);
		result.Combine(evalResult);

		long relative = evalResult.ResultObject - (_locationCounter + instructionLength);

		// have to redo range checks on new relative value
		(long min, long max) = size switch
		{
			Constants.Size.Byte => (sbyte.MinValue, sbyte.MaxValue),
			Constants.Size.Word => (short.MinValue, short.MaxValue),
			_ => (long.MinValue, long.MaxValue)
		};

		long truncated = size switch
		{
			Constants.Size.Byte => (byte)relative,
			Constants.Size.Word => (ushort)relative,
			_ => relative
		};

		if (relative < min || relative > max)
		{
			Token firstToken = operand.ExpressionTokens[0];
			result.AddWarning(
				firstToken.SourceLocation,
				Constants.ErrorCode.TruncatedValue,
				$"relative value {relative} truncated to Signed {size} {truncated}"
			);
		}

		result.ResultObject = truncated;
		return result;
	}

	private static int GetIncrementBit(Operand operand)
	{
		return operand switch
		{
			BlockOperand { Block: Constants.Block.I } => 1,
			BlockOperand { Block: Constants.Block.D } => 0,
			_ => throw new Exception($"Unexpected operand type {operand.GetType()} for increment/decrement selector")
		};
	}

	private static int GetRepeatBit(Operand operand)
	{
		return operand switch
		{
			BlockOperand { Block: Constants.Block.R } => 1,
			BlockOperand { Block: Constants.Block.S } => 0,
			_ => throw new Exception($"Unexpected operand type {operand.GetType()} for repeat/single selector")
		};
	}

	private static Result<int> GetBlockSizeSelector(Operand operand)
	{
		Result<int> result = new(0);

		Constants.Size size = operand switch
		{
			SizeOperand so => so.Size,
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 1 }] } => Constants.Size.Byte,
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 2 }] } => Constants.Size.Word,
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 4 }] } => Constants.Size.Dword,
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 8 }] } => Constants.Size.Qword,
			_ => Constants.Size.Unsized
		};

		if (size is not (Constants.Size.Byte or Constants.Size.Word))
		{
			result.AddError(
				operand.SourceLocation,
				Constants.ErrorCode.InvalidSize,
				"block instructions only support byte and word sizes"
			);
			return result;
		}

		result.ResultObject = GetSizeSelectorBits(size);
		return result;
	}

	private static int GetScaleOperandSelector(Operand operand)
	{
		return operand switch
		{
			SizeOperand so => GetSizeSelectorBits(so.Size),
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 1 }] } => GetSizeSelectorBits(
				Constants.Size.Byte
			),
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 2 }] } => GetSizeSelectorBits(
				Constants.Size.Word
			),
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 4 }] } => GetSizeSelectorBits(
				Constants.Size.Dword
			),
			ExpressionOperand { ExpressionTokens: [NumberToken { Value: 8 }] } => GetSizeSelectorBits(
				Constants.Size.Qword
			),
			_ => throw new Exception($"Unexpected operand type {operand.GetType()} for LEA scale selector")
		};
	}
}
