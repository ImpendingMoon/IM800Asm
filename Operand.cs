namespace IM800Asm
{
	internal class Operand
	{
		public Operand(int line, int column, Constants.OperandType type)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = string.Empty;
			ExpressionTokens = [];
		}

		public Operand(int line, int column, Constants.OperandType type, string lexeme)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = lexeme;
			ExpressionTokens = [];
		}
		public Operand(int line, int column, Constants.OperandType type, string lexeme)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = lexeme;
			ExpressionTokens = [];
		}

		public Constants.OperandType Type { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public string Lexeme { get; set; }

		public Constants.Size? Size { get; set; }
		public Constants.Instruction? Instruction { get; set; }
		public Constants.Directive? Directive { get; set; }
		public Constants.Register? Register { get; set; }
		public Constants.Condition? Condition { get; set; }
		public Constants.BlockOperand? BlockOperand { get; set; }
		public List<Token> ExpressionTokens { get; set; }
	}
}