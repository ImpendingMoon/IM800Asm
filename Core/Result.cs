namespace IM800Asm.Core;

/// <summary>
///     For actions where failure is an expected result.
/// </summary>
public class Result
{
	private readonly List<Error> _errors = [];
	private readonly List<Error> _warnings = [];

	public bool IsSuccess => Errors.Count == 0;

	public IReadOnlyList<Error> Errors => _errors;
	public IReadOnlyList<Error> Warnings => _warnings;

	/// <summary>Adds an error to this result.</summary>
	public void AddError(SourceLocation location, Constants.ErrorCode code, string message)
	{
		_errors.Add(new Error(location, code, Constants.ErrorSeverity.Error, message));
	}

	/// <summary>Adds a warning to this result.</summary>
	public void AddWarning(SourceLocation location, Constants.ErrorCode code, string message)
	{
		_warnings.Add(new Error(location, code, Constants.ErrorSeverity.Warning, message));
	}

	/// <summary>Appends the messages of another result to this result.</summary>
	public void Combine(Result other)
	{
		_errors.AddRange(other._errors);
		_warnings.AddRange(other._warnings);
	}

	public class Error(
		SourceLocation sourceLocation,
		Constants.ErrorCode code,
		Constants.ErrorSeverity severity,
		string message
	)
	{
		public Constants.ErrorCode Code { get; } = code;
		public Constants.ErrorSeverity Severity { get; } = severity;
		public string Message { get; } = message;
		public SourceLocation SourceLocation { get; } = sourceLocation;

		public override string ToString()
		{
			string severity = Severity switch
			{
				Constants.ErrorSeverity.Warning => "W",
				Constants.ErrorSeverity.Error => "E",
				_ => string.Empty
			};

			return $"{severity}{(int)Code:000}: {SourceLocation} {Message}";
		}
	}
}

/// <summary>
///     For actions where failure is an expected result.
///     Includes a member for returning an object.
/// </summary>
/// <typeparam name="T">Type of the result object.</typeparam>
public class Result<T>(T result) : Result
{
	public T ResultObject { get; set; } = result;
}
