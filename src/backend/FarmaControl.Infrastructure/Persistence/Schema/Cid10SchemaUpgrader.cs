using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Persistence.Schema;

public static class Cid10SchemaUpgrader
{
    public static async Task ApplyAsync(
        FarmaControlDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "cid10_codes" (
                "id" INTEGER NOT NULL CONSTRAINT "PK_cid10_codes" PRIMARY KEY AUTOINCREMENT,
                "code" TEXT NOT NULL,
                "name" TEXT NOT NULL,
                "created_at" TEXT NOT NULL
            );

            DROP INDEX IF EXISTS "IX_cid10_codes_code_name";
            CREATE INDEX IF NOT EXISTS "IX_cid10_codes_code" ON "cid10_codes" ("code");
            CREATE INDEX IF NOT EXISTS "IX_cid10_codes_name" ON "cid10_codes" ("name");
            CREATE INDEX IF NOT EXISTS "IX_cid10_codes_code_name" ON "cid10_codes" ("code", "name");
            """,
            cancellationToken);
    }
}
