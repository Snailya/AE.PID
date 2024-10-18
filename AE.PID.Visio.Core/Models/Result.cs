namespace AE.PID.Visio.Core.Models;

public class Result
{
    protected Result()
    {
        Exception = null;
    }

    protected Result(Exception exception)
    {
        Exception = exception;
    }

    public Exception? Exception { get; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess => Exception == null;

    public static Result Success(string? message = null)
    {
        return new Result { Message = message ?? string.Empty };
    }

    public static Result Failure(Exception exception)
    {
        return new Result(exception);
    }
}

public class Result<T> : Result
{
    private Result(T value)
    {
        Value = value;
    }

    private Result(Exception exception) : base(exception)
    {
    }


    public T? Value { get; }

    public static Result<T> Success(T value, string? message = null)
    {
        return new Result<T>(value) { Message = message ?? string.Empty };
    }

    public new static Result<T> Failure(Exception exception)
    {
        return new Result<T>(exception);
    }
}