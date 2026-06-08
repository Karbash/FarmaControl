using FarmaControl.Domain.Users;

namespace FarmaControl.Tests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_NormalizesEmailAndAllowsAuthentication()
    {
        User user = CreateUser();

        Assert.Equal("admin@teste.com", user.Email);
        Assert.True(user.CanAuthenticate);
        Assert.True(user.IsActive);
        Assert.False(user.IsDeleted);
    }

    [Fact]
    public void RevokeAccess_BlocksAuthentication()
    {
        User user = CreateUser();

        user.RevokeAccess(99, "Saiu da equipe");

        Assert.False(user.CanAuthenticate);
        Assert.NotNull(user.AccessRevokedAt);
        Assert.Equal(99, user.AccessRevokedByUserId);
    }

    [Fact]
    public void RestoreAccess_AllowsAuthenticationWhenActive()
    {
        User user = CreateUser();
        user.RevokeAccess(99, "Saiu da equipe");

        user.RestoreAccess();

        Assert.True(user.CanAuthenticate);
        Assert.Null(user.AccessRevokedAt);
        Assert.Null(user.AccessRevokedByUserId);
    }

    [Fact]
    public void SoftDelete_BlocksAuthenticationWithoutRemovingIdentity()
    {
        User user = CreateUser();

        user.SoftDelete(99);

        Assert.False(user.CanAuthenticate);
        Assert.False(user.IsActive);
        Assert.True(user.IsDeleted);
        Assert.NotNull(user.DeletedAt);
        Assert.Equal(99, user.DeletedByUserId);
    }

    [Fact]
    public void SoftDeletedUser_CannotBeChanged()
    {
        User user = CreateUser();
        user.SoftDelete(99);

        Assert.Throws<InvalidOperationException>(() =>
            user.UpdateProfile("Outro", "outro@teste.com", UserRole.Gerente));
    }

    [Fact]
    public void RevokeModule_KeepsAccessRecordAndMarksRevoked()
    {
        User user = CreateUser();
        user.GrantModule("Atendimentos", 99);

        user.RevokeModule("atendimentos", 99, "Sem escala");

        UserModuleAccess access = Assert.Single(user.ModuleAccesses);
        Assert.Equal("atendimentos", access.Module);
        Assert.True(access.IsRevoked);
        Assert.NotNull(access.RevokedAt);
    }

    private static User CreateUser()
    {
        return User.Create(
            "Administrador",
            " ADMIN@TESTE.COM ",
            "hash-forte",
            UserRole.Admin);
    }
}
