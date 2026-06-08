using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence.Schema;

public static class UserSchemaUpgrader
{
    public static async Task ApplyAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int signatureHashExists = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM pragma_table_info('users') WHERE name = 'signature_password_hash'")
            .SingleAsync(cancellationToken);

        if (signatureHashExists == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"users\" ADD COLUMN \"signature_password_hash\" TEXT NULL;",
                cancellationToken);
        }

        int resetRequiredExists = await dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(1) AS Value FROM pragma_table_info('users') WHERE name = 'signature_password_reset_required'")
            .SingleAsync(cancellationToken);

        if (resetRequiredExists == 0)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"users\" ADD COLUMN \"signature_password_reset_required\" INTEGER NOT NULL DEFAULT 1;",
                cancellationToken);
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE \"users\" SET \"role\" = 'atendente' WHERE lower(\"role\") = 'atendimento';",
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE \"users\" SET \"role\" = 'enfermeira' WHERE lower(\"role\") IN ('enfermagem', 'enfermeiro');",
            cancellationToken);
    }
}
