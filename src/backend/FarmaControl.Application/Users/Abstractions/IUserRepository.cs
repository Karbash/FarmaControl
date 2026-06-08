using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Users.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListAsync(bool includeDeleted, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListCareTeamAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListResponsibleUsersAsync(CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
