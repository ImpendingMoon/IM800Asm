namespace IM800Asm;

internal class InstructionTable
{
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

	private static List<Entry> MakeStandardRRMEntries(int opcode, bool allowMixedSizes = false)
	{
		return [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.R,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				0,
				allowMixedSizes
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.R,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				0,
				allowMixedSizes
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				0,
				allowMixedSizes
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				0,
				allowMixedSizes
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				0,
				allowMixedSizes
			),
		];
	}

	private static List<Entry> MakeStandardURUMEntries(int opcode, int function)
	{
		return [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				function
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.UM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				opcode,
				function
			),
		];
	}

	private static List<Entry> MakeStandardBLKEntries(int opcode, int function)
	{
		return [
		new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Block),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.BLK,
				[
					Constants.Size.Unsized, // Size is selected with a dedicated operand
				],
				Constants.Size.Unsized,
				opcode,
				function
			),
		];
	}

	private static List<Entry> MakeStandardMEntries(int opcode, int function, Constants.Size size)
	{
		return [
			new Entry(
				[
				],
				Constants.InstructionFormat.M,
				[
					size,
				],
				size,
				opcode,
				function
			)
		];
	}

	private static readonly Dictionary<Constants.Instruction, List<Entry>> _instructionTable = new()
	{
		// R/RM
		[Constants.Instruction.LD] =
		[
			.. MakeStandardRRMEntries(0b00000),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.I),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b0001,
				0b00000110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.A),
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.R),
				],
				Constants.InstructionFormat.M,
				[
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b0001,
				0b00001010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.R),
					new OperandSlot(Constants.AcceptedOperandType.ExactRegister, Constants.Register.A),
				],
				Constants.InstructionFormat.M,
				[
					Constants.Size.Word,
				],
				Constants.Size.Word,
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
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b000001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b00001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b00001
			),
		],
		[Constants.Instruction.IN] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b00010
			),
		],
		[Constants.Instruction.OUT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b00010
			),
		],
		[Constants.Instruction.ADD] = MakeStandardRRMEntries(0b00100),
		[Constants.Instruction.ADC] = MakeStandardRRMEntries(0b00101),
		[Constants.Instruction.SUB] = MakeStandardRRMEntries(0b00110),
		[Constants.Instruction.SBC] = MakeStandardRRMEntries(0b00111),
		[Constants.Instruction.AND] = MakeStandardRRMEntries(0b01000),
		[Constants.Instruction.OR] = MakeStandardRRMEntries(0b01001),
		[Constants.Instruction.XOR] = MakeStandardRRMEntries(0b01010),
		[Constants.Instruction.CP] = MakeStandardRRMEntries(0b01011),
		[Constants.Instruction.TST] = MakeStandardRRMEntries(0b01100),
		[Constants.Instruction.LEA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.WideRegister),
					new OperandSlot(Constants.AcceptedOperandType.NarrowRegister),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.R,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b10000,
				0,
				allowMixedSizes: true // LEA adds a 16-bit value to a 32-bit value
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.WideRegister),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.R,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b10000,
				0,
				allowMixedSizes: true
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.WideRegister),
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b10000,
				0,
				allowMixedSizes: true
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.NarrowRegister),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b10000,
				0,
				allowMixedSizes: true
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
					new OperandSlot(Constants.AcceptedOperandType.Size),
				],
				Constants.InstructionFormat.RM,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b10000,
				0,
				allowMixedSizes: true
			),
		],
		[Constants.Instruction.BIT] = MakeStandardRRMEntries(0b10001, allowMixedSizes: true),
		[Constants.Instruction.SET] = MakeStandardRRMEntries(0b10010, allowMixedSizes: true),
		[Constants.Instruction.RES] = MakeStandardRRMEntries(0b10011, allowMixedSizes: true),
		[Constants.Instruction.RLC] = MakeStandardRRMEntries(0b10100, allowMixedSizes: true),
		[Constants.Instruction.RRC] = MakeStandardRRMEntries(0b10101, allowMixedSizes: true),
		[Constants.Instruction.RL] = MakeStandardRRMEntries(0b10110, allowMixedSizes: true),
		[Constants.Instruction.RR] = MakeStandardRRMEntries(0b10111, allowMixedSizes: true),
		[Constants.Instruction.SLA] = MakeStandardRRMEntries(0b11000, allowMixedSizes: true),
		[Constants.Instruction.SRA] = MakeStandardRRMEntries(0b11001, allowMixedSizes: true),
		[Constants.Instruction.SRL] = MakeStandardRRMEntries(0b11010, allowMixedSizes: true),
		// UR/UM
		[Constants.Instruction.EXA] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
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
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b0000,
				0b001
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.UR,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
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
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b0000,
				0b010
			),
		],
		[Constants.Instruction.EXH] = MakeStandardURUMEntries(0b0000, 0b100),
		[Constants.Instruction.EXT] = MakeStandardURUMEntries(0b0000, 0b101),
		[Constants.Instruction.INC] = MakeStandardURUMEntries(0b0000, 0b110),
		[Constants.Instruction.DEC] = MakeStandardURUMEntries(0b0000, 0b111),
		[Constants.Instruction.CPL] = MakeStandardURUMEntries(0b0001, 0b000),
		[Constants.Instruction.NEG] = MakeStandardURUMEntries(0b0001, 0b001),
		[Constants.Instruction.MLT] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.UR,
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b0001,
				0b010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.UM,
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
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
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b0001,
				0b011
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.UM,
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
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
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
				0b0001,
				0b100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Memory),
				],
				Constants.InstructionFormat.UM,
				[
					Constants.Size.Word,
					Constants.Size.Dword,
				],
				Constants.Size.Word,
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
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00000
			),
		],
		[Constants.Instruction.JP] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00010
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00010
			),
		],
		[Constants.Instruction.CR] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00100
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Byte,
					Constants.Size.Word,
				],
				Constants.Size.Word,
				0b00100
			),
		],
		[Constants.Instruction.CALL] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.AnyRegister),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00110
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Dword,
				],
				Constants.Size.Dword,
				0b00110
			),
		],
		[Constants.Instruction.RET] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Unsized,
				],
				Constants.Size.Unsized,
				0b01000
			),
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Condition),
				],
				Constants.InstructionFormat.B,
				[
					Constants.Size.Unsized,
				],
				Constants.Size.Unsized,
				0b01000
			),
		],
		// M
		[Constants.Instruction.SCF] = MakeStandardMEntries(0b0000, 0b00000000, Constants.Size.Unsized),
		[Constants.Instruction.CCF] = MakeStandardMEntries(0b0000, 0b00000001, Constants.Size.Unsized),
		[Constants.Instruction.DAA] = MakeStandardMEntries(0b0000, 0b00000010, Constants.Size.Byte),
		[Constants.Instruction.RLD] = MakeStandardMEntries(0b0000, 0b00000011, Constants.Size.Byte),
		[Constants.Instruction.RRD] = MakeStandardMEntries(0b0000, 0b00000100, Constants.Size.Byte),
		[Constants.Instruction.RST] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				[
					Constants.Size.Unsized,
				],
				Constants.Size.Unsized,
				0b0000,
				0b00000101
			),
		],
		[Constants.Instruction.EXX] = MakeStandardMEntries(0b0001, 0b00000000, Constants.Size.Unsized),
		[Constants.Instruction.EXI] = MakeStandardMEntries(0b0001, 0b00000001, Constants.Size.Unsized),
		[Constants.Instruction.EI] = MakeStandardMEntries(0b0001, 0b00000010, Constants.Size.Unsized),
		[Constants.Instruction.DI] = MakeStandardMEntries(0b0001, 0b00000011, Constants.Size.Unsized),
		[Constants.Instruction.IM] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.M,
				[
					Constants.Size.Unsized,
				],
				Constants.Size.Unsized,
				0b0001,
				0b00000100
			),
		],
		[Constants.Instruction.RETI] = MakeStandardMEntries(0b0001, 0b00000111, Constants.Size.Unsized),
		[Constants.Instruction.RETN] = MakeStandardMEntries(0b0001, 0b00001000, Constants.Size.Unsized),
		[Constants.Instruction.HALT] = MakeStandardMEntries(0b0001, 0b00001001, Constants.Size.Unsized),
		// SB
		[Constants.Instruction.NOP] = [
			new Entry(
				[
				],
				Constants.InstructionFormat.SB,
				[
					Constants.Size.Unsized,
				],
				Constants.Size.Unsized,
				0b0000
			),
		],
		[Constants.Instruction.DJNZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				[
					Constants.Size.Byte,
				],
				Constants.Size.Byte,
				0b0001
			),
		],
		[Constants.Instruction.JAZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				[
					Constants.Size.Byte,
				],
				Constants.Size.Byte,
				0b0010
			),
		],
		[Constants.Instruction.JANZ] = [
			new Entry(
				[
					new OperandSlot(Constants.AcceptedOperandType.Immediate),
				],
				Constants.InstructionFormat.SB,
				[
					Constants.Size.Byte,
				],
				Constants.Size.Byte,
				0b0011
			),
		],
		// BLK
		[Constants.Instruction.BLD] = MakeStandardBLKEntries(0b0000, 0b000),
		[Constants.Instruction.BCP] = MakeStandardBLKEntries(0b0000, 0b001),
		[Constants.Instruction.BTST] = MakeStandardBLKEntries(0b0000, 0b010),
		[Constants.Instruction.BIN] = MakeStandardBLKEntries(0b0000, 0b011),
		[Constants.Instruction.BOUT] = MakeStandardBLKEntries(0b0000, 0b100),
	};

	public class Entry
	{
		public Entry(
			List<OperandSlot> slots,
			Constants.InstructionFormat instructionFormat,
			List<Constants.Size> allowedSizes,
			Constants.Size defaultSize,
			int opcode,
			int function = 0,
			bool allowMixedSizes = false
		)
		{
			Slots = slots;
			InstructionFormat = instructionFormat;
			AllowedSizes = allowedSizes;
			DefaultSize = defaultSize;
			Opcode = opcode;
			Function = function;
			AllowMixedSizes = allowMixedSizes;
		}

		public List<OperandSlot> Slots { get; set; }
		public Constants.InstructionFormat InstructionFormat { get; set; }
		public List<Constants.Size> AllowedSizes { get; set; }
		public Constants.Size DefaultSize { get; set; }
		public int Opcode { get; set; }
		public int Function { get; set; }

		/// <summary>
		/// Used so that basic size validation doesn't immediately reject this.
		/// Custom sizing must be handled by the format measure/emit methods.
		/// </summary>
		public bool AllowMixedSizes { get; set; }

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