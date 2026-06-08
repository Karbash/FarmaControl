using FarmaControl.Application.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence;

public sealed class SqliteDatabaseHealthCheck(FarmaControlDbContext dbContext)
    : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return CanOpenConnectionAsync(cancellationToken);
    }

    private async Task<bool> CanOpenConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = dbContext.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
