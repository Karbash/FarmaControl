namespace FarmaControl.Application.Abstractions;

public sealed record AppError(string Code, string Message)
{
    public static AppError Validation(string message) => new("validation", message);

    public static AppError NotFound(string message) => new("not_found", message);

    public static AppError Forbidden(string message) => new("forbidden", message);

    public static AppError Conflict(string message) => new("conflict", message);
}
