using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Auth;

namespace FarmaControl.Application.Users.Models;

public sealed record LoginModel(string Email, string Password) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add(AppError.Validation("Email e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            errors.Add(AppError.Validation("Senha e obrigatoria."));
        }

        return errors;
    }

    public string NormalizedEmail()
    {
        return Email.Trim().ToLowerInvariant();
    }

    public static LoginModel FromRequest(LoginRequest request)
    {
        return new LoginModel(request.Email, request.Password);
    }
}
