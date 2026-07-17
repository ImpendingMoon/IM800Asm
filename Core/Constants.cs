namespace IM800Asm.Core;

public static class Constants
{
	public enum AcceptedOperandType
	{
		// Accepts any of A, B, C, D, E, H, L, AF, BC, DE, HL, IX, IY, SP
		AnyRegister,

		// Accepts any of A, B, C, D, E, H, L
		NarrowRegister,

		// Accepts any of AF, BC, DE, HL, IX, IY, SP
		WideRegister,

		// Accepts only one register
		ExactRegister,

		// Accepts any of [rr], [rr+#], [#] (there are no instructions that only take a subset of these)
		Memory,

		// Accepts an expression
		Immediate,

		// Accepts a condition or the C register (fixed to Carry condition)
		Condition,

		// Accepts a block operand or the D, I, R registers (fixed to Decrement, Increment, Repeat operands)
		Block,

		// Accepts a size operand or the 1, 2, 4, or 8 number literals
		Size
	}

	public enum Block
	{
		D, I, // Decrement, Increment
		S, R // Single, Repeat
	}

	public enum Condition
	{
		NZ, Z, NC, C, PO, PE, P, M
	}

	public enum Directive
	{
		ORG, EQU, ALIGN,
		DEFB, DEFW, DEFD, DEFQ, DEFS, DB, DW, DD, DQ, DS,
		RESB, RESW, RESD, RESQ, RB, RW, RD, RQ
	}

	public enum ErrorCode
	{
		None = 0,

		// Lexer
		UnexpectedCharacter = 100,
		EmptyCharacterLiteral = 101,
		UnterminatedCharacterLiteral = 102,
		CharacterLiteralTooLong = 103,
		UnterminatedStringLiteral = 104,
		InvalidNumberLiteral = 105,
		NumberLiteralTooLarge = 106,
		InvalidEscapeSequence = 107,

		// Parser
		UnexpectedToken = 200,
		UnexpectedEndOfFile = 201,
		ExpectedOperand = 202,
		UnexpectedOperand = 203,
		UnterminatedIndirectOperand = 204,

		// Semantic
		RedefinedSymbol = 300,
		InvalidAddressingMode = 301,
		InvalidOperand = 302,
		ValueOutOfRange = 303,
		InvalidContext = 304,
		InvalidSize = 305,
		OperandSizeMismatch = 306,

		// Expressions
		InvalidExpression = 400,
		UndefinedSymbol = 401,
		DivisionByZero = 402,
		UnmatchedParenthesis = 403,
		TruncatedValue = 404
	}

	public enum ErrorSeverity
	{
		Info = 0,
		Warning,
		Error
	}

	public enum Instruction
	{
		LD, EX, PUSH, POP, LEA, EXH, EXA, EXX, EXI, IN, OUT,
		ADD, ADC, SUB, SBC, CP, INC, DEC, NEG, EXT, MLT, DIV, SDIV, DAA,
		AND, OR, XOR, TST, CPL, BIT, SET, RES, RLC, RRC, RL, RR, SLA, SRA, SRL, RLD, RRD,
		NOP, JP, JR, DJNZ, JAZ, JANZ, CALL, CR, RET, RST, SCF, CCF,
		EI, DI, IM, RETI, RETN, HALT,
		BLD, BCP, BTST, BIN, BOUT,
		BKPT
	}

	public enum InstructionFormat
	{
		R,
		RM,
		UR,
		UM,
		B,
		M,
		SB,
		BLK
	}

	public enum Register
	{
		A, F, B, C, D, E, H, L,
		AF, BC, DE, HL, IX, IY, SP,
		PC, I, R
	}

	public enum Signedness
	{
		Either,
		Signed,
		Unsigned
	}

	public enum Size
	{
		Unsized = 0, Byte, Word, Dword, Qword
	}

	public enum SymbolType
	{
		Label,
		EQU
	}

	public enum TokenType
	{
		Unknown = 0,
		Colon,
		Comma,
		LParen,
		RParen,
		LBracket,
		RBracket,
		Plus,
		Minus,
		Star,
		Slash,
		Percent,
		ShiftLeft,
		ShiftRight,
		Ampersand,
		Pipe,
		Caret,
		Tilde,
		Equal,
		NotEqual,
		Greater,
		Less,
		GreaterEqual,
		LessEqual,
		Exclamation,
		Dollar,
		NewLine,
		EndOfFile
	}

	public const string HexPrefix = "0x";
	public const string BinaryPrefix = "0b";
	public const string OctalPrefix = "0o";

	public const char HexSuffix = 'h';
	public const char BinarySuffix = 'b';
	public const char OctalSuffix = 'o';

	public const int DecimalRadix = 10;
	public const int HexRadix = 16;
	public const int BinaryRadix = 2;
	public const int OctalRadix = 8;

	public const char CommentChar = ';';
	public const char StringDelim = '"';
	public const char SizeSeparator = '.';

	public const string ModuloAlias = "MOD";
	public const string ShiftLeftAlias = "SHL";
	public const string ShiftRightAlias = "SHR";

	// Condition field encoding for an unconditional B instruction
	public const int AlwaysConditionSelector = 0b1111;

	public static readonly HashSet<Register> AnyRegisterValues =
	[
		Register.A,
		Register.B,
		Register.C,
		Register.D,
		Register.E,
		Register.H,
		Register.L,
		Register.AF,
		Register.BC,
		Register.DE,
		Register.HL,
		Register.IX,
		Register.IY,
		Register.SP
	];

	public static readonly HashSet<Register> NarrowRegisterValues =
	[
		Register.A,
		Register.B,
		Register.C,
		Register.D,
		Register.E,
		Register.H,
		Register.L,
		Register.R
	];

	public static readonly HashSet<Register> WideRegisterValues =
	[
		Register.AF,
		Register.BC,
		Register.DE,
		Register.HL,
		Register.IX,
		Register.IY,
		Register.SP,
		Register.I
	];

	public static readonly HashSet<Instruction> BitAndShiftInstructions =
	[
		Instruction.BIT,
		Instruction.SET,
		Instruction.RES,
		Instruction.RLC,
		Instruction.RRC,
		Instruction.RL,
		Instruction.RR,
		Instruction.SLA,
		Instruction.SRA,
		Instruction.SRL
	];
}
