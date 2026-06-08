using FarmaControl.Contracts.Auth;
using FarmaControl.Contracts.Users;
using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Users.Models;

public static class UserModel
{
    public static UserResponse FromDomain(User user)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.Value,
            user.IsActive,
            user.IsDeleted,
            user.CanAuthenticate,
            user.CanSign,
            user.SignaturePasswordResetRequired,
            user.AccessRevokedAt,
            user.AccessRevokedByUserId,
            user.AccessRevocationReason,
            user.DeletedAt,
            user.DeletedByUserId,
            user.ModuleAccesses.Select(ModuleFromDomain).ToArray(),
            user.CreatedAt,
            user.UpdatedAt);
    }

    public static AuthenticatedUserResponse ToAuthenticatedResponse(User user)
    {
        return new AuthenticatedUserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.Role.Value,
            user.SignaturePasswordResetRequired,
            EffectiveModules(user).Order().ToArray());
    }

    private static UserModuleAccessResponse ModuleFromDomain(UserModuleAccess access)
    {
        return new UserModuleAccessResponse(
            access.Id,
            access.Module,
            access.IsRevoked,
            access.RevokedAt,
            access.RevokedByUserId,
            access.RevocationReason,
            access.GrantedByUserId,
            access.CreatedAt);
    }

    private static IEnumerable<string> EffectiveModules(User user)
    {
        var modules = new HashSet<string>(
            user.ModuleAccesses
                .Where(access => !access.IsRevoked)
                .Select(access => access.Module),
            StringComparer.OrdinalIgnoreCase);

        foreach (string module in DefaultModulesForRole(user.Role))
        {
            modules.Add(module);
        }

        return modules;
    }

    private static IEnumerable<string> DefaultModulesForRole(UserRole role)
    {
        if (role == UserRole.Atendente || role == UserRole.Medico || role == UserRole.Enfermeira)
        {
            yield return "atendimentos";
        }

        if (role == UserRole.Farmaceutico)
        {
            yield return "atendimentos";
            yield return "estoque";
        }

        if (
            role == UserRole.Movimentacao ||
            role == UserRole.Entrada ||
            role == UserRole.Saida ||
            role == UserRole.Visualizacao)
        {
            yield return "estoque";
        }
    }
}
