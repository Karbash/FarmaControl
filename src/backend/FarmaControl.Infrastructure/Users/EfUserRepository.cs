using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Domain.Users;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Users;

public sealed class EfUserRepository(FarmaControlDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return UsersWithModules()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        return UsersWithModules()
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken)
    {
        IQueryable<User> query = UsersWithModules().AsNoTracking();

        if (!includeDeleted)
        {
            query = query.Where(user => !user.IsDeleted);
        }

        return await query
            .OrderBy(user => user.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListCareTeamAsync(CancellationToken cancellationToken)
    {
        HashSet<string> careRoles =
        [
            UserRole.Medico.Value,
            UserRole.Enfermeira.Value,
            UserRole.Farmaceutico.Value
        ];

        User[] users = await UsersWithModules()
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                user.IsActive &&
                user.AccessRevokedAt == null)
            .ToArrayAsync(cancellationToken);

        return users
            .Where(user => careRoles.Contains(user.Role.Value))
            .OrderBy(user => user.Role.Value)
            .ThenBy(user => user.Name)
            .ToArray();
    }

    public async Task<IReadOnlyList<User>> ListResponsibleUsersAsync(CancellationToken cancellationToken)
    {
        HashSet<string> responsibleRoles =
        [
            UserRole.Admin.Value,
            UserRole.Gerente.Value,
            UserRole.Medico.Value,
            UserRole.Enfermeira.Value,
            UserRole.Farmaceutico.Value,
            UserRole.Movimentacao.Value,
            UserRole.Entrada.Value,
            UserRole.Saida.Value
        ];

        User[] users = await UsersWithModules()
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                user.IsActive &&
                user.AccessRevokedAt == null)
            .ToArrayAsync(cancellationToken);

        return users
            .Where(user => responsibleRoles.Contains(user.Role.Value))
            .OrderBy(user => user.Name)
            .ToArray();
    }

    public Task<bool> EmailExistsAsync(
        string email,
        long? exceptUserId,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        return dbContext.Users.AnyAsync(
            user =>
                user.Email == normalizedEmail &&
                !user.IsDeleted &&
                (!exceptUserId.HasValue || user.Id != exceptUserId.Value),
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> UsersWithModules()
    {
        return dbContext.Users
            .Include(user => user.ModuleAccesses);
    }
}
