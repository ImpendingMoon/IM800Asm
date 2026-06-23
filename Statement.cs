using System.Reflection.Metadata;

namespace IM800Asm
{
	internal class Statement
	{
		public Statement(int line, int column, Constants.StatementType type)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = string.Empty;
			Text = string.Empty;
			Operands = [];
		}

		public Statement(int line, int column, Constants.StatementType type, string lexeme)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = lexeme;
			Text = lexeme;
			Operands = [];
		}

		public Statement(int line, int column, Constants.StatementType type, string lexeme, string text)
		{
			Line = line;
			Column = column;
			Type = type;
			Lexeme = lexeme;
			Text = text;
			Operands = [];
		}

		public Constants.StatementType Type;
		public int Line { get; set; }
		public int Column { get; set; }

		/// <summary>
		/// Original text from the source code
		/// </summary>
		public string Lexeme;

		/// <summary>
		/// Processed text from the grammar parser
		/// </summary>
		public string Text;

		public Constants.Instruction? Instruction { get; set; }
		public Constants.Directive? Directive { get; set; }
		public Constants.Size? Size { get; set; }

		public List<Operand> Operands { get; set; }

		public override string ToString()
		{
			return $"{Line}:{Column}:\t{Type}: Lexeme: {Lexeme}, Text: {Text}";
		}
	}
}