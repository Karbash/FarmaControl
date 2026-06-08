using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Users;
using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Users.Models;

public sealed record UpdateUserModel(
    string Name,
    string Email,
    string? Password,
    string Role,
    bool IsActive) : IExplicitModel
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

        if (!TryParseRole(Role, out _))
        {
            errors.Add(AppError.Validation("Role invalida."));
        }

        return errors;
    }

    public void ApplyTo(User user, string? passwordHash)
    {
        user.UpdateProfile(Name, Email, UserRole.From(Role));

        if (IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        if (!string.IsNullOrWhiteSpace(passwordHash))
        {
            user.ChangePasswordHash(passwordHash);
        }
    }

    public static UpdateUserModel FromRequest(UpdateUserRequest request)
    {
        return new UpdateUserModel(
            request.Name,
            request.Email,
            request.Password,
            request.Role,
            request.IsActive);
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
