namespace IM800Asm;

internal class InstructionTable
{
	// instruction class
	// Instruction
	// Size (as in size field)
	// Format (switches what measure/emit functions are used)
	// Operands
	// allowed sizes
	// allow mixed size (property or just in the measure/emit. probably property since many instructions allow)
	// base address
	// length in bytes
	// maybe could just be in the statements class?
	// but it's a lot of new information that could be stored separately

	public static bool TryResolveInstruction(InstructionStatement statement, out Entry? resolvedEntry)
	{
		resolvedEntry = null;

		if (!_instructionTable.ContainsKey(statement.Instruction))
		{
			throw new Exception($"Instruction {statement.Instruction} has not been added to the instruction table.");
		}

		List<Entry> entries = _instructionTable[statement.Instruction];

		foreach (Entry entry in entries)
		{
			if (entry.Matches(statement.Operands))
			{
				resolvedEntry = entry;
				return true;
			}
		}

		return false;
	}

	private static readonly Dictionary<Constants.Instruction, List<Entry>> _instructionTable = new()
	{
		// R/RM
		[Constants.Instruction.LD] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b000000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b000000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b00000
			),
			// LD I, #; LD A, R; LD R, A
			// Don't need to be first in priority since special registers I, R are excluded from AnyRegister matches        
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.I),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.A),
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.R),
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00001010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.R),
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.A),
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00001011
			),
		],
		[Constants.Instruction.EX] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b000001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00001
			),
		],
		[Constants.Instruction.IN] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00010
			),
		],
		[Constants.Instruction.OUT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00010
			),
		],
		[Constants.Instruction.ADD] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b00100
			),
		],
		[Constants.Instruction.ADC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b00101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b00101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b00101
			),
		],
		[Constants.Instruction.SUB] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b00110
			),
		],
		[Constants.Instruction.SBC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b00111
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b00111
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b00111
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b00111
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b00111
			),
		],
		[Constants.Instruction.AND] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b01000
			),
		],
		[Constants.Instruction.OR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b01001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b01001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b01001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b01001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b01001
			),
		],
		[Constants.Instruction.XOR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b01010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b01010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b01010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b01010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b01010
			),
		],
		[Constants.Instruction.CP] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b01011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b01011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b01011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b01011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b01011
			),
		],
		[Constants.Instruction.TST] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b01100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b01100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b01100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b01100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b01100
			),
		],
		[Constants.Instruction.LEA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.WideRegister),
					new OperandSlot(Constants.AcceptedOperandType.NarrowRegister),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.R,
				0b10000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.WideRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.RM,
				0b10000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.NarrowRegister),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.RM,
				0b10000
			),
		],
		[Constants.Instruction.BIT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10001
			),
		],
		[Constants.Instruction.SET] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10010
			),
		],
		[Constants.Instruction.RES] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10011
			),
		],
		[Constants.Instruction.RLC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10100
			),
		],
		[Constants.Instruction.RRC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10101
			),
		],
		[Constants.Instruction.RL] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
		],
		[Constants.Instruction.RR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b10110
			),
		],
		[Constants.Instruction.SLA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b11000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b11000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b11000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b11000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b11000
			),
		],
		[Constants.Instruction.SRA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b11001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b11001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b11001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b11001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b11001
			),
		],
		[Constants.Instruction.SRL] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				0b11010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				0b11010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.RM,
				0b11010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				0b11010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				0b11010
			),
		],
		// UR/UM
		[Constants.Instruction.EXA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b000
			),
		],
		[Constants.Instruction.PUSH] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b001
			),
		],
		[Constants.Instruction.POP] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b010
			),
		],
		[Constants.Instruction.EXH] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b100
			),
		],
		[Constants.Instruction.EXT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b101
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b101
			),
		],
		[Constants.Instruction.INC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b110
			),
		],
		[Constants.Instruction.DEC] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0000,
				0b111
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b111
			),
		],
		[Constants.Instruction.CPL] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0001,
				0b000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b000
			),
		],
		[Constants.Instruction.NEG] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0001,
				0b001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b001
			),
		],
		[Constants.Instruction.MLT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0001,
				0b010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b010
			),
		],
		[Constants.Instruction.DIV] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0001,
				0b011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b011
			),
		],
		[Constants.Instruction.SDIV] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				0b0001,
				0b100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Indirect),
				],
				Constants.InstructionFormat.UM,
				0b0000,
				0b100
			),
		],
		// B
		[Constants.Instruction.JR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00000
			),
		],
		[Constants.Instruction.JP] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00010
			),
						new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00010
			),
		],
		[Constants.Instruction.CR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00100
			),
						new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00100
			),
		],
		[Constants.Instruction.CALL] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				0b00110
			),
		],
		[Constants.Instruction.RET] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.B,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
				],
				Constants.InstructionFormat.B,
				0b01000
			),
		],
		// M
		[Constants.Instruction.SCF] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000000
			),
		],
		[Constants.Instruction.CCF] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000001
			),
		],
		[Constants.Instruction.DAA] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000010
			),
		],
		[Constants.Instruction.RLD] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000011
			),
		],
		[Constants.Instruction.RRD] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000100
			),
		],
		[Constants.Instruction.RST] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				0b0000,
				0b00000101
			),
		],
		[Constants.Instruction.EXX] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000000
			),
		],
		[Constants.Instruction.EXI] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000001
			),
		],
		[Constants.Instruction.EI] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000010
			),
		],
		[Constants.Instruction.DI] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000011
			),
		],
		[Constants.Instruction.IM] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000100
			),
		],
		[Constants.Instruction.RETI] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00000111
			),
		],
		[Constants.Instruction.RETN] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00001000
			),
		],
		[Constants.Instruction.HALT] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				0b0001,
				0b00001001
			),
		],
		// SB
		[Constants.Instruction.NOP] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.SB,
				0b0000
			),
		],
		[Constants.Instruction.DJNZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				0b0001
			),
		],
		[Constants.Instruction.JAZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				0b0010
			),
		],
		[Constants.Instruction.JANZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				0b0011
			),
		],
		// BLK
		[Constants.Instruction.BLD] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				0b0000,
				0b000
			),
		],
		[Constants.Instruction.BCP] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				0b0000,
				0b001
			),
		],
		[Constants.Instruction.BTST] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				0b0000,
				0b010
			),
		],
		[Constants.Instruction.BIN] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				0b0000,
				0b011
			),
		],
		[Constants.Instruction.BOUT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				0b0000,
				0b100
			),
		],
	};

	public class Entry
	{
		public Entry(
			List<OperandSlot> slots,
			Constants.InstructionFormat instructionFormat,
			int opcode,
			int function = 0
		)
		{
			Slots = slots;
			InstructionFormat = instructionFormat;
			Opcode = opcode;
			Function = function;
		}

		public List<OperandSlot> Slots { get; set; }
		public Constants.InstructionFormat InstructionFormat { get; set; }
		public int Opcode { get; set; }
		public int Function { get; set; }

		public bool Matches(List<Operand> operands)
		{
			if (Slots.Count != operands.Count)
			{
				return false;
			}

			for (int i = 0; i < Slots.Count; i++)
			{
				OperandSlot slot = Slots[i];
				Operand operand = operands[i];

				if (!slot.Matches(operand))
				{
					return false;
				}
			}

			return true;
		}
	}
}