namespace VibeTravels.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<Error> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Ok() => new(true, Array.Empty<Error>());

    public static Result Fail(Error error) => new(false, new[] { error });

    public static Result Fail(IReadOnlyList<Error> errors)
        => errors.Count == 0
            ? new(false, new[] { ResultErrors.Unknown() })
            : new(false, errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors) : base(isSuccess, errors)
        => Value = value;

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<Error>());

    public static new Result<T> Fail(Error error) => new(false, default, new[] { error });

    public static new Result<T> Fail(IReadOnlyList<Error> errors)
        => errors.Count == 0
            ? new(false, default, new[] { ResultErrors.Unknown() })
            : new(false, default, errors);
}
