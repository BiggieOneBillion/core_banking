namespace CoreBanking.Application.Common.Models;

public record class Result
{
    public bool IsSuccess { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();

    public string Message { get; init; } = string.Empty;

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(params string[] errors) => new() { IsSuccess = false, Errors = errors };


}

public record Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    public static new Result<T> Failure(params string[] errors) => new() { IsSuccess = false, Errors = errors };
}
