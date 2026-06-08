namespace FarmaControl.Application.Abstractions;

public static class ModelValidation
{
    public static bool HasErrors(this IReadOnlyList<AppError> errors)
    {
        return errors.Count > 0;
    }

    public static AppError FirstOrDefaultError(this IReadOnlyList<AppError> errors)
    {
        return errors.Count > 0
            ? errors[0]
            : AppError.Validation("Modelo invalido.");
    }
}
