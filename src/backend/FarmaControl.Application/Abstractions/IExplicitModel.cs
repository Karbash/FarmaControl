namespace FarmaControl.Application.Abstractions;

public interface IExplicitModel
{
    IReadOnlyList<AppError> Validate();
}
