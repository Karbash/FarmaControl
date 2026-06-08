using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Users;
using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Users.Models;

public sealed record CreateUserModel(
    string Name,
    string Email,
    string Password,
    string Role) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(AppError.Validation("Nome e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add(AppError.Validation("Email e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            errors.Add(AppError.Validation("Senha e obrigatoria."));
        }

        if (!TryParseRole(Role, out _))
        {
            errors.Add(AppError.Validation("Role invalida."));
        }

        return errors;
    }

    public User ToDomain(string passwordHash)
    {
        return User.Create(Name, Email, passwordHash, UserRole.From(Role));
    }

    public static CreateUserModel FromRequest(CreateUserRequest request)
    {
        return new CreateUserModel(request.Name, request.Email, request.Password, request.Role);
    }

    private static bool TryParseRole(string role, out UserRole? userRole)
    {
        try
        {
            userRole = UserRole.From(role);
            return true;
        }
        catch
        {
            userRole = null;
            return false;
        }
    }
}
