using System.Reflection;
using FarmaControl.Application.Users.Models;
using FarmaControl.Domain.Users;

namespace FarmaControl.Tests.Application;

public sealed class UserModelTests
{
    [Fact]
    public void ToAuthenticatedResponse_MedicoReceivesAtendimentosModuleByRole()
    {
        User user = CreateUser(UserRole.Medico);

        var response = UserModel.ToAuthenticatedResponse(user);

        Assert.Contains("atendimentos", response.Modules);
    }

    [Fact]
    public void ToAuthenticatedResponse_FarmaceuticoReceivesCareAndInventoryModulesByRole()
    {
        User user = CreateUser(UserRole.Farmaceutico);

        var response = UserModel.ToAuthenticatedResponse(user);

        Assert.Contains("atendimentos", response.Modules);
        Assert.Contains("estoque", response.Modules);
    }

    [Fact]
    public void ToAuthenticatedResponse_KeepsExplicitGrantedModules()
    {
        User user = CreateUser(UserRole.Medico);
        SetId(user, 10);
        user.GrantModule("auditoria", 10);

        var response = UserModel.ToAuthenticatedResponse(user);

        Assert.Contains("auditoria", response.Modules);
        Assert.Contains("atendimentos", response.Modules);
    }

    private static User CreateUser(UserRole role)
    {
        return User.Create("Profissional", $"{role.Value}@teste.com", "hash", role);
    }

    private static void SetId(object entity, long id)
    {
        PropertyInfo property = entity.GetType().BaseType!.GetProperty("Id")!;
        property.SetValue(entity, id);
    }
}
