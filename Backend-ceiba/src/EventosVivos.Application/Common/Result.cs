namespace EventosVivos.Application.Common;

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, int StatusCode)
{
    public static Result<T> Success(T value, int statusCode = 200) => new(true, value, null, statusCode);
    public static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
}
