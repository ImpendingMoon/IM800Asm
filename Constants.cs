namespace IM800Asm;

internal static class Constants
{
	public const string HexPrefix = "0x";
	public const string BinaryPrefix = "0b";
	public const string OctalPrefix = "0o";

	public const char HexSuffix = 'h';
	public const char BinarySuffix = 'b';
	public const char OctalSuffix = 'o';

	public const int RadixDecimal = 10;
	public const int RadixHex = 16;
	public const int RadixBinary = 2;
	public const int RadixOctal = 8;

	public const char CommentChar = ';';
	public const char StringDelim = '"';
	public const char SizeSeparator = '.';

	public enum TokenType
	{
		Unknown = 0,
		Identifier,
		Number,
		String,
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
		EndOfFile,
	}

	public enum Instruction
	{
		LD, EX, PUSH, POP, LEA, EXA, EXX, EXI, IN, OUT,
		ADD, ADC, SUB, SBC, CP, INC, DEC, NEG, EXT, MLT, DIV, SDIV, DAA,
		AND, OR, XOR, TST, CPL, BIT, SET, RES, RLC, RRC, RL, RR, SLA, SRA, SRL, RLD, RRD,
		NOP, JP, JR, DJNZ, JAZ, JANZ, CALL, CR, RET, RST, SCF, CCF,
		EI, DI, IM, RETI, RETN, HALT,
		BLD, BCP, BTST, BIN, BOUT,
	}

	public enum Directive
	{
		ORG, EQU, ALIGN,
		DEFB, DEFW, DEFD, DEFQ, DEFS, DB, DW, DD, DQ, DS,
		RESB, RESW, RESD, RESQ, RB, RW, RD, RQ,
	}

	public enum Register
	{
		A, F, B, C, D, E, H, L,
		AF, BC, DE, HL, IX, IY, SP,
		PC, I, R,
	}

	public enum Condition
	{
		NZ, Z, NC, C, PO, PE, P, M,
	}

	public enum BlockIncrement
	{
		D, I,
	}

	public enum BlockRepeat
	{
		S, R,
	}

	public enum Size
	{
		BYTE, WORD, DWORD, QWORD,
	}
}