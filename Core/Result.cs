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
	public void AddError(string source, string message)
	{
		_errors.Add(new Error(source, message));
	}

	/// <summary>Adds a warning to this result.</summary>
	public void AddWarning(string source, string message)
	{
		_warnings.Add(new Error(source, message));
	}

	/// <summary>Appends the messages of another result to this result.</summary>
	public void Combine(Result other)
	{
		_errors.AddRange(other._errors);
		_warnings.AddRange(other._warnings);
	}

	public class Error(string source, string message)
	{
		public string Source { get; set; } = source;
		public string Message { get; set; } = message;

		public override string ToString()
		{
			return $"{Source}: {Message}";
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
