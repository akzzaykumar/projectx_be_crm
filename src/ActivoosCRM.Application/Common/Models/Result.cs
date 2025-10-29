namespace ActivoosCRM.Application.Common.Models;

public class Result<T>
{
    public bool Success { get; private set; }
    public bool IsFailure => !Success;
    public T? Data { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string[] Errors { get; private set; } = Array.Empty<string>();

    private Result(bool success, T? data, string message, string[] errors)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors;
    }

    public static Result<T> CreateSuccess(T data, string message = "")
    {
        return new Result<T>(true, data, message, Array.Empty<string>());
    }

    public static Result<T> CreateFailure(string message)
    {
        return new Result<T>(false, default, message, new[] { message });
    }

    public static Result<T> CreateFailure(string[] errors)
    {
        var message = string.Join("; ", errors);
        return new Result<T>(false, default, message, errors);
    }

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return Success ? onSuccess(Data!) : onFailure(Message);
    }
}

public class Result
{
    public bool Success { get; private set; }
    public bool IsFailure => !Success;
    public string Message { get; private set; } = string.Empty;
    public string[] Errors { get; private set; } = Array.Empty<string>();

    private Result(bool success, string message, string[] errors)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }

    public static Result CreateSuccess(string message = "")
    {
        return new Result(true, message, Array.Empty<string>());
    }

    public static Result CreateFailure(string message)
    {
        return new Result(false, message, new[] { message });
    }

    public static Result CreateFailure(string[] errors)
    {
        var message = string.Join("; ", errors);
        return new Result(false, message, errors);
    }
}