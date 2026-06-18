namespace HandoraApplication.Helpers;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string[]? Errors { get; private set; }

    private Result(bool isSuccess, T? data, string[]? errors)
    {
        IsSuccess = isSuccess;
        Data = data;
        Errors = errors;
    }

    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, null);
    }

    public static Result<T> Failure(params string[] errors)
    {
        return new Result<T>(false, default, errors);
    }
}
public class Result
{
    public bool IsSuccess { get; private set; }
    public string[]? Errors { get; private set; }

    private Result(bool isSuccess, string[]? errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success()
    {
        return new Result(true, null);
    }

    public static Result Failure(params string[] errors)
    {
        return new Result(false, errors);
    }
}