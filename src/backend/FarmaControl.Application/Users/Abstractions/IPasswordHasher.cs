using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Users.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(User user, string password);

    bool VerifyHash(string passwordHash, string password);
}
