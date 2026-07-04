namespace IM800Asm;

internal class OperandSlot
{
	public OperandSlot(Constants.AcceptedOperandType acceptedOperandType, Constants.Register? exactRegister = null)
	{
		if (acceptedOperandType == Constants.AcceptedOperandType.ExactRegister && exactRegister is null)
		{
			throw new ArgumentException("ExactRegister operand slot requires a specified register");
		}

		AcceptedOperandType = acceptedOperandType;
		ExactRegister = exactRegister;
	}

	public Constants.AcceptedOperandType AcceptedOperandType { get; set; }
	public Constants.Register? ExactRegister { get; set; }

	public bool Matches(Operand operand)
	{
		return AcceptedOperandType switch
		{
			Constants.AcceptedOperandType.AnyRegister =>
				operand is RegisterOperand ro &&
				Constants.AnyRegisterValues.Contains(ro.Register),

			Constants.AcceptedOperandType.NarrowRegister =>
				operand is RegisterOperand ro &&
				Constants.NarrowRegisterValues.Contains(ro.Register),

			Constants.AcceptedOperandType.WideRegister =>
				operand is RegisterOperand ro &&
				Constants.WideRegisterValues.Contains(ro.Register),

			Constants.AcceptedOperandType.ExactRegister =>
				operand is RegisterOperand ro &&
				ro.Register == ExactRegister,

			Constants.AcceptedOperandType.Indirect =>
				operand is IndirectRegisterOperand or IndirectExpressionOperand or IndexedOperand,

			Constants.AcceptedOperandType.Immediate => operand is ExpressionOperand,

			Constants.AcceptedOperandType.Condition =>
				operand is ConditionOperand ||
				operand is RegisterOperand
				{
					Register: Constants.Register.C
				},

			Constants.AcceptedOperandType.Block =>
				operand is BlockOperand ||
				operand is RegisterOperand
				{
					Register: Constants.Register.D
						or Constants.Register.I
						or Constants.Register.R
				},

			Constants.AcceptedOperandType.Size => IsSizeOperand(operand),

			_ => false
		};
	}

	private static bool IsSizeOperand(Operand operand)
	{
		return operand is SizeOperand || operand is ExpressionOperand
		{
			ExpressionTokens:
			[
				NumberToken { Value: 1 or 2 or 4 or 8 }
			]
		};
	}



}