namespace IM800Asm;

internal partial class Assembler
{
	/// <summary>
	/// Evaluates pass one directives, and measures pass two directives
	/// </summary>
	/// <param name="st"></param>
	/// <returns>Number of bytes to increment the location counter by</returns>
	private Result<long> EmitDirectivePassOne(DirectiveStatement st)
	{
		return st.Directive switch
		{
			Constants.Directive.ORG => EmitOrgDirective(st),
			Constants.Directive.EQU => EmitEquDirective(st),
			Constants.Directive.ALIGN => EmitAlignDirective(st),
			Constants.Directive.DB or Constants.Directive.DEFB => MeasureDefineValue(st, Constants.Size.Byte),
			Constants.Directive.DW or Constants.Directive.DEFW => MeasureDefineValue(st, Constants.Size.Word),
			Constants.Directive.DD or Constants.Directive.DEFD => MeasureDefineValue(st, Constants.Size.Dword),
			Constants.Directive.DQ or Constants.Directive.DEFQ => MeasureDefineValue(st, Constants.Size.Qword),
			Constants.Directive.DS or Constants.Directive.DEFS => MeasureDefineValue(st, Constants.Size.Byte),
			Constants.Directive.RB or Constants.Directive.RESB => EmitReserveSpace(st, Constants.Size.Byte),
			Constants.Directive.RW or Constants.Directive.RESW => EmitReserveSpace(st, Constants.Size.Word),
			Constants.Directive.RD or Constants.Directive.RESD => EmitReserveSpace(st, Constants.Size.Dword),
			Constants.Directive.RQ or Constants.Directive.RESQ => EmitReserveSpace(st, Constants.Size.Qword),
			_ => throw new Exception($"unknown directive {st.Directive}"),
		};
	}

	/// <summary>
	/// Evaluates pass directives, and evaluates pass two directives
	/// </summary>
	/// <param name="st"></param>
	/// <returns>Number of bytes to increment the location counter by</returns>
	private Result<long> EmitDirectivePassTwo(DirectiveStatement st)
	{
		return st.Directive switch
		{
			Constants.Directive.ORG => EmitOrgDirective(st), // This could be a lookup instead of re-evaluate
			Constants.Directive.EQU => new(0), // Does not affect LC, already evaluated
			Constants.Directive.ALIGN => EmitAlignDirective(st), // same lookup thing
			Constants.Directive.DB or Constants.Directive.DEFB => EmitDefineValue(st, Constants.Size.Byte),
			Constants.Directive.DW or Constants.Directive.DEFW => EmitDefineValue(st, Constants.Size.Word),
			Constants.Directive.DD or Constants.Directive.DEFD => EmitDefineValue(st, Constants.Size.Dword),
			Constants.Directive.DQ or Constants.Directive.DEFQ => EmitDefineValue(st, Constants.Size.Qword),
			Constants.Directive.DS or Constants.Directive.DEFS => EmitDefineValue(st, Constants.Size.Byte),
			Constants.Directive.RB or Constants.Directive.RESB => EmitReserveSpace(st, Constants.Size.Byte),
			Constants.Directive.RW or Constants.Directive.RESW => EmitReserveSpace(st, Constants.Size.Word),
			Constants.Directive.RD or Constants.Directive.RESD => EmitReserveSpace(st, Constants.Size.Dword),
			Constants.Directive.RQ or Constants.Directive.RESQ => EmitReserveSpace(st, Constants.Size.Qword),
			_ => throw new Exception($"unknown directive {st.Directive}"),
		};
	}

	private Result<long> MeasureDefineValue(DirectiveStatement st, Constants.Size size)
	{
		Result<long> result = new(0);

		if (st.Operands.Count == 0)
		{
			result.AddError("Assembler", $"{st.Line}:{st.Column}:\t{st.Directive} expected operand list");
			return result;
		}

		int multiplier = size switch
		{
			Constants.Size.Byte => 1,
			Constants.Size.Word => 2,
			Constants.Size.Dword => 4,
			Constants.Size.Qword => 8,
			_ => throw new Exception($"unknown size {size}"),
		};

		result.ResultObject = st.Operands.Count * multiplier;
		return result;
	}

	private Result<long> EmitDefineValue(DirectiveStatement st, Constants.Size size)
	{
		Result<long> result = new(0);

		if (st.Operands.Count == 0)
		{
			result.AddError("Assembler", $"{st.Line}:{st.Column}:\t{st.Directive} expected operand list");
			return result;
		}

		foreach (Operand operand in st.Operands)
		{
			if (operand is not ExpressionOperand eo)
			{
				result.AddError(
					"Assembler",
					$"{operand.Line}:{operand.Column}:\tinvalid operand for directive {st.Directive}"
				);
				return result;
			}

			Result<long> evalResult = _evaluator.Evaluate(
				eo.ExpressionTokens,
				_locationCounter,
				size,
				Constants.Signedness.Either
			);

			result.Combine(evalResult);
			EmitValue(evalResult.ResultObject, size);
		}

		int multiplier = size switch
		{
			Constants.Size.Byte => 1,
			Constants.Size.Word => 2,
			Constants.Size.Dword => 4,
			Constants.Size.Qword => 8,
			_ => throw new Exception($"unknown size {size}"),
		};

		result.ResultObject = st.Operands.Count * multiplier;
		return result;
	}

	private Result<long> EmitOrgDirective(DirectiveStatement st)
	{
		Result<long> result = new(0);

		if (st.Operands.Count != 1 || st.Operands[0] is not ExpressionOperand eo)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\tinvalid operands for directive {st.Directive}"
			);
			return result;
		}

		Result<long> evalResult = _evaluator.Evaluate(
			eo.ExpressionTokens,
			_locationCounter,
			Constants.Size.Qword,
			Constants.Signedness.Signed
		);

		result.Combine(evalResult);

		if (evalResult.ResultObject < 0)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression cannot be negative"
			);
			return result;
		}

		if (evalResult.ResultObject > uint.MaxValue)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression cannot be greater than 0x{uint.MaxValue:X}"
			);
			return result;
		}

		result.ResultObject = evalResult.ResultObject - _locationCounter;
		return result;
	}

	private Result<long> EmitEquDirective(DirectiveStatement st)
	{
		Result<long> result = new(0);

		if (st.Operands.Count != 1 || st.Operands[0] is not ExpressionOperand eo)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\tinvalid operands for directive {st.Directive}"
			);
			return result;
		}

		if (_lastDefinedSymbol == string.Empty)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} must follow a label declaration"
			);
			return result;
		}

		List<Token> tokens = eo.ExpressionTokens;
		Result<long> evaluationResult = _evaluator.Evaluate(
			tokens, _locationCounter,
			Constants.Size.Qword,
			Constants.Signedness.Signed
		);
		result.Combine(evaluationResult);

		RedefineSymbol(_lastDefinedSymbol, evaluationResult.ResultObject);
		return result;
	}

	private Result<long> EmitAlignDirective(DirectiveStatement st)
	{
		Result<long> result = new(0);

		if (st.Operands.Count != 1 || st.Operands[0] is not ExpressionOperand eo)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\tinvalid operands for directive {st.Directive}"
			);
			return result;
		}

		List<Token> tokens = eo.ExpressionTokens;
		Result<long> evalResult = _evaluator.Evaluate(
			tokens,
			_locationCounter,
			Constants.Size.Qword,
			Constants.Signedness.Signed
		);
		result.Combine(evalResult);

		if (evalResult.ResultObject <= 0)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression must be positive"
			);
			return result;
		}

		if (evalResult.ResultObject > uint.MaxValue)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression cannot be greater than 0x{uint.MaxValue:X}"
			);
			return result;
		}

		// Calculate number of bytes to add to _locationCounter to get to alignment
		long alignment = evalResult.ResultObject;
		long alignedCounter = (_locationCounter + alignment - 1) / alignment * alignment;
		result.ResultObject = alignedCounter - _locationCounter;

		return result;
	}

	private Result<long> EmitReserveSpace(DirectiveStatement st, Constants.Size size)
	{
		Result<long> result = new(0);

		if (st.Operands.Count != 1 || st.Operands[0] is not ExpressionOperand eo)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\tinvalid operands for directive {st.Directive}"
			);
			return result;
		}

		Result<long> evalResult = _evaluator.Evaluate(
			eo.ExpressionTokens,
			_locationCounter,
			Constants.Size.Qword,
			Constants.Signedness.Signed
		);
		result.Combine(evalResult);

		if (evalResult.ResultObject < 0)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression cannot be negative"
			);
			return result;
		}

		if (evalResult.ResultObject > uint.MaxValue)
		{
			result.AddError(
				"Assembler",
				$"{st.Line}:{st.Column}:\t{st.Directive} expression cannot be greater than 0x{uint.MaxValue:X}"
			);
			return result;
		}

		int multiplier = size switch
		{
			Constants.Size.Byte => 1,
			Constants.Size.Word => 2,
			Constants.Size.Dword => 4,
			Constants.Size.Qword => 8,
			_ => throw new Exception($"unknown size {size}"),
		};

		result.ResultObject = evalResult.ResultObject * multiplier;
		return result;
	}
}